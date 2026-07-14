using AionLightning.Commons.Utils;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.GameObjects.Siege;
using AionLightning.Game.Model.Siege;
using AionLightning.Game.Model.Templates.Spawns;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services.Siege.Assault;

/// <summary>
/// Java services.siegeservice.FortressAssault — the balaur auto-assault state machine that runs against
/// a fortress currently under siege: after an initial random delay it "spawns the dredgion" (flavor
/// announcement only in this port, see <see cref="BalaurAssaultService.SpawnDredgion"/>), then after a
/// further random delay drops a wave of balaur attackers in two concentric rings around the siege boss
/// plus any nearby static peace-mode balaur spawns.
/// </summary>
public sealed class FortressAssault : Assault
{
    private readonly bool _isBalaurea;
    private readonly BalaurAssaultService _owner;
    private readonly SpawnService _spawnService;
    private readonly IDataManager _dataManager;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly Action<SiegeNpc> _registerNpc;
    private readonly ILogger _log;

    private volatile bool _spawned;

    public FortressAssault(FortressSiege siege, BalaurAssaultService owner, SpawnService spawnService,
        IDataManager dataManager, PlayerConnectionRegistry connRegistry, Action<SiegeNpc> registerNpc, ILogger log)
        : base(siege)
    {
        _isBalaurea = WorldId != 400010000;
        _owner = owner;
        _spawnService = spawnService;
        _dataManager = dataManager;
        _connRegistry = connRegistry;
        _registerNpc = registerNpc;
        _log = log;
    }

    protected override void ScheduleAssault(int delaySeconds)
    {
        AssaultCts = new CancellationTokenSource();
        _ = RunScheduleAsync(delaySeconds, AssaultCts.Token);
    }

    /// <summary>Java's ThreadPoolManager.schedule(dredgionTask) → nested schedule(spawnTask) chain,
    /// collapsed into one async method sharing a single cancellation token (see <see cref="Assault.AssaultCts"/>).</summary>
    private async Task RunScheduleAsync(int delaySeconds, CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
            _owner.SpawnDredgion(GetSpawnIdByFortressId());

            await Task.Delay(TimeSpan.FromSeconds(Rnd.Get(240, 300)), ct);
            SpawnAttackers();
        }
        catch (OperationCanceledException)
        {
            // finished/cancelled before this stage fired — matches Java's Future.cancel(true) no-op path.
        }
        catch (Exception e)
        {
            _log.LogError(e, "Error running balaur assault schedule for fortress {Id}", LocationId);
        }
    }

    protected override void OnAssaultFinish(bool captured)
    {
        if (!_spawned) return;

        if (!captured)
            RewardDefendingPlayers();
        else
            BroadcastToFortress(SM_SYSTEM_MESSAGE.AbyssDragonBossKilled());
    }

    /// <summary>
    /// Java spawnAttackers() — lays out <c>amount</c> attackers on two concentric rings (radius1 inner,
    /// radius2 outer) centered on the siege boss, spanning a 180° arc from the boss's facing.
    /// note: Java additionally tried to reuse coordinates gathered by <see cref="SpawnRegularBalaurs"/>
    /// (<c>spawnLocations</c>) for roughly the last two-thirds of the ring, then immediately overwrote the
    /// just-spawned attacker's x/y/z back to the ring coordinate via <c>attacker.getSpawn().setX/Y/Z(...)</c>
    /// — but Java's VisibleObject position is captured from the spawn template once at construction time
    /// and never re-reads the template afterward, so that post-spawn mutation has no visible effect; every
    /// attacker ends up placed at its ring coordinate regardless of which branch ran. This port spawns
    /// directly at the ring coordinate for every index, skipping the dead bookkeeping.
    /// </summary>
    private void SpawnAttackers()
    {
        if (_spawned) return;
        _spawned = true;

        // note: SpawnAttackers is only ever scheduled after Siege.InitSiegeBoss succeeded (Boss is always
        // set by the time OnSiegeStartAsync returns and NotifyBalaurAssaultStart fires) — this guard is
        // pure defense in depth against the still-open InitSiegeBoss gap documented on Siege.Boss itself.
        if (Boss is not { } boss) return;

        SpawnRegularBalaurs();

        var bossPos = boss.Npc.Position;
        float x = bossPos.X, y = bossPos.Y, z = bossPos.Z;
        byte heading = (byte)boss.Npc.HomePosition.Heading;

        int radius1 = _isBalaurea ? 5 : Rnd.Get(7, 13);
        int radius2 = _isBalaurea ? 9 : Rnd.Get(15, 20);
        int amount = _isBalaurea ? Rnd.Get(30, 50) : Rnd.Get(20, 30);

        float minAngle = heading * 3f - 90f;
        if (minAngle < 0) minAngle += 360f;
        double minRadian = minAngle * Math.PI / 180.0;
        float interval = (float)(Math.PI / (amount / 2));

        var idList = GetSpawnIds();
        int commanderCount = _isBalaurea ? 0 : Rnd.Get(2);

        for (int i = 0; i < amount; i++)
        {
            float x1, y1;
            if (i < amount / 2)
            {
                x1 = (float)(Math.Cos(minRadian + interval * i) * radius1);
                y1 = (float)(Math.Sin(minRadian + interval * i) * radius1);
            }
            else
            {
                x1 = (float)(Math.Cos(minRadian + interval * (i - amount / 2)) * radius2);
                y1 = (float)(Math.Sin(minRadian + interval * (i - amount / 2)) * radius2);
            }

            int templateId = i <= commanderCount ? idList[0] : idList[Rnd.Get(1, idList.Count - 1)];
            SpawnAttacker(templateId, x + x1, y + y1, z, heading);
        }

        BroadcastToFortress(SM_SYSTEM_MESSAGE.AbyssCarrierDropDragon());
    }

    /// <summary>
    /// Java spawnRegularBalaurs() — spawns every static peace-mode balaur siege-spawn point near the boss
    /// as an extra defending army.
    /// note: Java additionally filtered out AbyssNpcType.ARTIFACT/TELEPORTER templates here — that field
    /// isn't part of this port's NPC static data (see SiegeNpc.IsBoss's doc comment on the same gap), so
    /// this only filters by siege race + PEACE mod. Java also recorded each spawned location into
    /// <c>spawnLocations</c> for reuse by <see cref="SpawnAttackers"/> — dropped here, see that method's
    /// note explaining why it was dead bookkeeping.
    /// </summary>
    private void SpawnRegularBalaurs()
    {
        foreach (var template in _dataManager.SiegeSpawns.GetSiegeSpawnsBySiegeId(LocationId))
        {
            if (template.SiegeRace != SiegeRace.BALAUR || template.SiegeModType != SiegeModType.PEACE) continue;
            SpawnAttacker(template.NpcId, template.X + 2, template.Y + 2, template.Z, template.Heading);
        }
    }

    /// <summary>Java SpawnEngine.addNewSiegeSpawn(...) + SpawnEngine.spawnObject(...) — spawns one
    /// ASSAULT-mod, BALAUR-race siege NPC at an arbitrary computed position and registers it the same way
    /// SiegeService.SpawnNpcs registers its own data-driven siege spawns, so the location's normal
    /// DeSpawnNpcs(locationId) call at siege end also cleans these up.</summary>
    private void SpawnAttacker(int npcId, float x, float y, float z, byte heading)
    {
        var template = new SiegeSpawnTemplate
        {
            SiegeId = LocationId,
            SiegeRace = SiegeRace.BALAUR,
            SiegeModType = SiegeModType.ASSAULT,
            WorldId = WorldId,
            NpcId = npcId,
            X = x,
            Y = y,
            Z = z,
            Heading = heading,
        };

        if (_spawnService.SpawnSiegeNpc(template) is not { } siegeNpc) return;
        _registerNpc(siegeNpc);

        var infoPacket = new SM_NPC_INFO(siegeNpc.Npc);
        var scope = siegeNpc.Npc.Position;
        _ = Task.Run(async () =>
        {
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer is { } p && p.Position.SameScope(scope))
                    try { await conn.SendAsync(infoPacket); } catch { }
        });
    }

    /// <summary>Approximates Java's siegeLocation.doOnAllPlayers(Visitor) — that zone/knownlist broadcast
    /// helper isn't ported yet (see FortressSiege's own doc comments on the same gap), so this falls back
    /// to "everyone currently on this fortress's map", matching the substitute pattern SiegeService itself
    /// already uses elsewhere (e.g. BroadcastRiftAsync).</summary>
    private void BroadcastToFortress(SM_SYSTEM_MESSAGE message)
    {
        _ = Task.Run(async () =>
        {
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer is { } p && p.Position.WorldId == WorldId)
                    try { await conn.SendAsync(message); } catch { }
        });
    }

    /// <summary>Java rewardDefendingPlayers() — Java's own TODO ("participating Players recieve Mail on
    /// Death of Commander") was never implemented upstream either; kept as a matching no-op stub.</summary>
    private void RewardDefendingPlayers()
    {
    }

    private int GetSpawnIdByFortressId() => LocationId switch
    {
        2011 => 5,
        2021 => 6,
        3021 => 10,
        3011 => 11,
        1141 => 12,
        1221 => 13,
        1131 => 15,
        1132 => 14,
        1241 => 16,
        1231 => 17,
        1211 => 18,
        1251 => 19,
        1011 => 20,
        _ => 1,
    };

    private List<int> GetSpawnIds()
    {
        var spawns = new List<int>();
        switch (LocationId)
        {
            case 1131: // Lower Abyss
            case 1132:
            case 1141:
                spawns.AddRange([
                    276649, // Commander
                    276767, 276766, 276720, 276719, 276717, 276715, 276714, 276710, 276709, 276699,
                    276690, 276689, 276687, 276684, 276680, 276679, 276675, 276674, 276672, 276670,
                    276669, 276668, 276667, 276665, 276664, 276663, 276660, 276659, 276658, 276655,
                    276654, 276653, 276645, 276644, 276643, 276640, 276639, 276638, 276635, 276634,
                    276633, 276632, 276630, 276629, 276628,
                ]);
                return spawns;
            case 1211: // Upper Abyss
            case 1221:
            case 1231:
            case 1241:
            case 1251:
            case 1011:
                spawns.AddRange([
                    276871, // Commander
                    277037, 277036, 277034, 277033, 277022, 277021, 277020, 277019, 277017, 277016,
                    277015, 277014, 277012, 277011, 277007, 277006, 277002, 277001, 276999, 276992,
                    276991, 276990, 276989, 276987, 276986, 276982, 276981, 276977, 276976, 276972,
                    276971, 276970, 276953, 276952, 276951, 276950, 276949, 276948, 276947, 276946,
                    276945, 276943, 276942, 276941, 276940, 276933, 276932, 276931, 276930, 276929,
                    276913, 276912, 276911, 276910, 276909, 276893, 276892, 276891, 276890, 276889,
                    276888, 276887, 276886, 276885, 276884, 276883, 276882, 276881, 276880, 276879,
                    276878, 276877, 276876, 276875, 276874, 276873, 276868, 276867, 276866, 276865,
                    276863, 276862, 276861, 276860, 276858, 276857, 276856, 276855, 276853, 276852,
                    276851, 276850, 276700,
                ]);
                return spawns;
            case 2011: // Balaurea Fortresses
            case 2021:
            case 3011:
            case 3021:
                spawns.AddRange([
                    258236, // Commander
                    // 258259 — Artifact Attacker (Java has this commented out too)
                    258246, 258245, 258244, 258243, 258242, 258241, 258240, 258239, 258238, 258237,
                ]);
                return spawns;
            case 5011:
            case 6011:
            case 6021:
                spawns.AddRange([272286, 272287, 272288, 272289, 272290, 272294, 272295, 272299, 272300, 272304, 272305]);
                return spawns;
            default:
                return spawns;
        }
    }
}
