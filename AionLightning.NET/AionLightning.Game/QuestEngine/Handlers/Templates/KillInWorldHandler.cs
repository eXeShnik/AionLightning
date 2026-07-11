using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest.Script;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.QuestEngine.Handlers.Templates;

/// <summary>
/// Data-driven "kill N while in these worlds" quest handler (Java
/// <c>questEngine.handlers.template.KillInWorld</c> port) — covers &lt;kill_in_world&gt; entries.
/// </summary>
/// <remarks>
/// PvP Phase 1 (2026-07-11 survey): Java's kill-count objective (<c>onKillInWorldEvent</c>,
/// incrementing quest var 0 up to <c>amount</c>) is driven by <c>PvpService.notifyKillQuests</c> — a
/// player-kills-opposing-race-player event, dispatched via <c>QuestEngine.onKillInWorld(worldId)</c>.
/// This is now wired: <c>Combat.Handlers.PvpKillHandler</c> calls
/// <c>QuestEngine.OnPlayerKillAsync</c> after the AP exchange, which looks up this handler's
/// world registration (<see cref="Register"/>) and calls <see cref="OnPlayerKillAsync"/> below.
/// Not yet ported (Phase 2, needs AggroList on Creature): Java also notifies every online
/// group/alliance member of the killer within range, not just the killer themself.
/// <c>invasion_world</c> (Java's Rift/Vortex-triggered auto-start via <c>onEnterWorldEvent</c>) is
/// still parsed for data completeness but not wired, for the same reason MonsterHuntScriptEntry's
/// aggro/invasion fields aren't (no RiftService/VortexService port yet).
/// </remarks>
public sealed class KillInWorldHandler : QuestHandlerBase
{
    private readonly HashSet<int> _startNpcs;
    private readonly HashSet<int> _endNpcs;
    private readonly HashSet<int> _worldIds;
    private readonly int          _killAmount;

    public KillInWorldHandler(KillInWorldScriptEntry data, IDataManager dataManager,
        IQuestDao questDao, QuestRewardService rewardService)
        : base(data.Id, dataManager, questDao, rewardService)
    {
        _startNpcs  = data.StartNpcIds;
        _endNpcs    = data.EndNpcIds;
        _worldIds   = data.WorldIds;
        _killAmount = data.Amount;
    }

    public override void Register(QuestEngine engine)
    {
        foreach (int npcId in _startNpcs)
        {
            var npc = engine.RegisterQuestNpc(npcId);
            npc.OnQuestStart.Add(QuestId);
            npc.OnTalk.Add(QuestId);
        }

        foreach (int npcId in _endNpcs)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);

        foreach (int worldId in _worldIds)
            engine.RegisterKillInWorld(worldId, QuestId);
    }

    /// <summary>
    /// Kill-count objective (Java <c>defaultOnKillRankedEvent(env, 0, killAmount, true)</c>):
    /// increments quest var 0 by one per qualifying kill until it reaches <c>killAmount - 1</c>, at
    /// which point the quest flips straight to REWARD (mirrors MonsterHunt's var/persist/
    /// SM_QUEST_ACTION handling via <see cref="QuestHandlerBase.ChangeQuestStepAsync"/>).
    /// </summary>
    public override async ValueTask<bool> OnPlayerKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var < _killAmount - 1)
        {
            await ChangeQuestStepAsync(conn, entry, varIdx: 0, newValue: var + 1, toReward: false, ct);
            return true;
        }
        if (var == _killAmount - 1)
        {
            await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var template = Template;
        if (template is null) return false;

        var player      = env.Player;
        int targetId     = env.TargetId;
        int targetObjId  = env.Target?.ObjectId ?? 0;
        var entry        = player.Quests.Get(QuestId);
        var status       = entry?.Status ?? QuestStatus.NONE;
        var dialog       = DialogActionLookup.FromId(env.DialogId);

        switch (status)
        {
            case QuestStatus.NONE:
                if (_startNpcs.Count > 0 && !_startNpcs.Contains(targetId)) return false;
                if (player.Level < template.MinLevel) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT => await SendQuestDialogAsync(conn, targetObjId, 4762, ct),
                    _ => await SendQuestStartDialogAsync(env, conn, ct),
                };

            case QuestStatus.REWARD:
                if (!_endNpcs.Contains(targetId)) return false;
                return await SendQuestEndDialogAsync(env, conn, ct);

            default:
                return false; // START: no dialog action here — kill progress advances via OnPlayerKillAsync
        }
    }
}
