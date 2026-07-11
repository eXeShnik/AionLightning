using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Summons;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

/// <summary>
/// M381 Phase 1: Spiritmaster-style &lt;summon&gt; skill effect — create/dismiss/mode-switch for the
/// single player-controlled summon a master may have out at a time (Java SummonsService).
/// Movement/combat ticking lives in <see cref="NpcAiService"/> alongside regular NPC AI.
/// </summary>
public sealed class SummonsService
{
    // Java ReleaseSummonTask: 5s between the RELEASE mode switch and the actual world removal.
    private static readonly TimeSpan ReleaseDelay = TimeSpan.FromSeconds(5);

    private readonly IDataManager _dataManager;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ILogger<SummonsService> _log;

    public SummonsService(IDataManager dataManager, GameWorld world,
        PlayerConnectionRegistry connRegistry, ILogger<SummonsService> log)
    {
        _dataManager  = dataManager;
        _world        = world;
        _connRegistry = connRegistry;
        _log          = log;
    }

    /// <summary>Spawns a summon at the master's current position. Refuses (Java msg 1300072) if the
    /// master already has one out — Phase 1 does not auto-release the old summon first.</summary>
    public async Task<bool> CreateSummonAsync(Player master, GsClientConnection masterConn,
        int npcId, int skillId, int skillLevel, int liveTimeSeconds, CancellationToken ct = default)
    {
        if (master.Summon is not null)
        {
            try { await masterConn.SendAsync(SM_SYSTEM_MESSAGE.SummonAlreadyHaveFollower(), ct); } catch { }
            return false;
        }

        var template = _dataManager.Npcs.GetTemplate(npcId);
        if (template is null)
        {
            _log.LogWarning("SummonsService: no NpcTemplate for npc_id {NpcId} (skill {SkillId})", npcId, skillId);
            return false;
        }

        var summon = new Summon(master, template, (byte)Math.Max(1, skillLevel), liveTimeSeconds)
        {
            ObjectId = ObjectIdFactory.Next(),
            Position = master.Position,
        };

        _world.Add(summon);
        master.Summon = summon;

        int worldId = master.Position.WorldId;
        var infoPkt  = new SM_NPC_INFO(summon);
        var emotePkt = new SM_EMOTION(summon, EmotionType.START_EMOTE2);
        foreach (var conn in _connRegistry.GetAll())
        {
            if (conn.ActivePlayer?.Position.WorldId != worldId) continue;
            try { await conn.SendAsync(infoPkt, ct); } catch { }
            try { await conn.SendAsync(emotePkt, ct); } catch { }
        }
        try { await masterConn.SendAsync(new SM_SUMMON_PANEL(summon), ct); } catch { }

        if (liveTimeSeconds > 0)
            _ = ScheduleAutoReleaseAsync(summon, liveTimeSeconds);

        return true;
    }

    private async Task ScheduleAutoReleaseAsync(Summon summon, int seconds)
    {
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        if (summon.Master is null) return; // already released some other way
        await ReleaseAsync(summon, UnsummonType.Unspecified);
    }

    /// <summary>Switches the summon's behavior mode, mirroring Java SummonsService.doMode. RELEASE
    /// routes to <see cref="ReleaseAsync"/> with UnsummonType.Command instead of setting the mode directly.</summary>
    public async Task DoModeAsync(Summon summon, SummonMode mode, int targetObjId, CancellationToken ct = default)
    {
        if (summon.IsAlreadyDead || summon.Master is null) return;

        if (mode == SummonMode.Release)
        {
            await ReleaseAsync(summon, UnsummonType.Command, ct);
            return;
        }

        if (mode == SummonMode.Attack)
        {
            var target = _world.GetNpcByObjectId(targetObjId);
            if (target is null) return; // stale/invalid client-supplied target — ignore (Java silently drops too)
            summon.Target = target;
        }
        else
        {
            summon.Target = null;
        }
        summon.Mode = mode;

        var masterConn = _connRegistry.Get(summon.Master.ObjectId);
        if (masterConn is null) return;

        var modeMsg = mode switch
        {
            SummonMode.Attack => SM_SYSTEM_MESSAGE.SummonAttackMode(summon.Name),
            SummonMode.Guard  => SM_SYSTEM_MESSAGE.SummonGuardMode(summon.Name),
            SummonMode.Rest   => SM_SYSTEM_MESSAGE.SummonRestMode(summon.Name),
            _                 => null,
        };
        if (modeMsg is not null)
            try { await masterConn.SendAsync(modeMsg, ct); } catch { }
        try { await masterConn.SendAsync(new SM_SUMMON_UPDATE(summon), ct); } catch { }
    }

    /// <summary>Two-phase dismissal (Java SummonsService.release + ReleaseSummonTask): flips to RELEASE mode
    /// immediately, then removes the summon from the world 5s later. COMMAND/DISTANCE notify the master;
    /// LOGOUT/UNSPECIFIED are silent (matches Java's switch in ReleaseSummonTask).</summary>
    public async Task ReleaseAsync(Summon summon, UnsummonType unsummonType, CancellationToken ct = default)
    {
        if (summon.Mode == SummonMode.Release) return;
        summon.Mode = SummonMode.Release;

        var master     = summon.Master;
        var masterConn = master is not null ? _connRegistry.Get(master.ObjectId) : null;
        if (masterConn is not null && unsummonType is UnsummonType.Command or UnsummonType.Distance)
        {
            try { await masterConn.SendAsync(new SM_SUMMON_UPDATE(summon), ct); } catch { }
        }

        _ = Task.Run(async () =>
        {
            await Task.Delay(ReleaseDelay);
            await FinishReleaseAsync(summon, unsummonType);
        });
    }

    private async Task FinishReleaseAsync(Summon summon, UnsummonType unsummonType)
    {
        _world.Remove(summon);
        var master = summon.Master;
        if (master is not null && ReferenceEquals(master.Summon, summon))
            master.Summon = null;
        summon.Master = null;

        int worldId = summon.Position.WorldId;
        var deletePkt = new SM_DELETE(summon.ObjectId);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(deletePkt); } catch { }

        if (unsummonType == UnsummonType.Logout || master is null) return;

        var masterConn = _connRegistry.Get(master.ObjectId);
        if (masterConn is null) return;
        try { await masterConn.SendAsync(SM_SYSTEM_MESSAGE.SummonUnsummonFollower(summon.Name)); } catch { }
        try { await masterConn.SendAsync(new SM_SUMMON_OWNER_REMOVE(summon.ObjectId)); } catch { }
        try { await masterConn.SendAsync(new SM_SUMMON_PANEL_REMOVE()); } catch { }
    }

    /// <summary>Master disconnected — deletes the summon immediately with no delay and no packets to the
    /// (already-gone) owner, only a zone-wide SM_DELETE (Java LOGOUT unsummon semantics).</summary>
    public async Task ReleaseImmediatelyAsync(Summon summon, CancellationToken ct = default)
    {
        _world.Remove(summon);
        var master = summon.Master;
        if (master is not null && ReferenceEquals(master.Summon, summon))
            master.Summon = null;
        summon.Master = null;
        summon.Mode   = SummonMode.Release;

        int worldId = summon.Position.WorldId;
        var deletePkt = new SM_DELETE(summon.ObjectId);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(deletePkt, ct); } catch { }
    }
}
