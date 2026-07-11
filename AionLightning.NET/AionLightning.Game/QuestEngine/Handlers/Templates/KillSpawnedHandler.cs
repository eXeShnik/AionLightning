using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest.Script;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.QuestEngine.Handlers.Templates;

/// <summary>
/// Data-driven "use a spawner object to spawn a mob, then kill it" quest handler (Java
/// <c>questEngine.handlers.template.KillSpawned</c> port) — covers &lt;kill_spawned&gt; entries.
/// </summary>
/// <remarks>
/// Java looks up the spawner npc's static spawn spot via <c>SPAWNS_DATA2.getFirstSpawnByNpcId</c>
/// to place the new monster. This port instead spawns at the spawner object's own live
/// <see cref="Position"/> (<see cref="Model.VisibleObject.Position"/>) — simpler, and always
/// instance/position-correct for the exact object the player interacted with.
/// </remarks>
public sealed class KillSpawnedHandler : QuestHandlerBase
{
    private readonly HashSet<int> _startNpcs;
    private readonly HashSet<int> _endNpcs;
    private readonly List<SpawnedMonsterEntry> _spawnedMonsters;
    private readonly HashSet<int> _spawnerObjectIds;
    private readonly SpawnService _spawnService;

    public KillSpawnedHandler(KillSpawnedScriptEntry data, IDataManager dataManager, IQuestDao questDao,
        QuestRewardService rewardService, SpawnService spawnService)
        : base(data.Id, dataManager, questDao, rewardService)
    {
        _startNpcs        = data.StartNpcIds;
        _endNpcs          = data.EndNpcIds;
        _spawnedMonsters  = data.SpawnedMonsters;
        _spawnerObjectIds = new HashSet<int>(_spawnedMonsters.Select(m => m.SpawnerObject));
        _spawnService     = spawnService;
    }

    public override void Register(QuestEngine engine)
    {
        foreach (int npcId in _startNpcs)
        {
            var npc = engine.RegisterQuestNpc(npcId);
            npc.OnQuestStart.Add(QuestId);
            npc.OnTalk.Add(QuestId);
        }

        foreach (var monster in _spawnedMonsters)
            foreach (int npcId in monster.NpcIds)
                engine.RegisterQuestNpc(npcId).OnKill.Add(QuestId);

        foreach (int npcId in _endNpcs)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);

        foreach (int npcId in _spawnerObjectIds)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
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
                    DialogAction.QUEST_SELECT => await SendQuestDialogAsync(conn, targetObjId, 1011, ct),
                    _ => await SendQuestStartDialogAsync(env, conn, ct),
                };

            case QuestStatus.START:
                if (_spawnerObjectIds.Contains(targetId))
                    return dialog == DialogAction.USE_OBJECT && TrySpawnMonster(env, targetId);

                if (!AllGroupsComplete(entry!)) return false;
                if (!_endNpcs.Contains(targetId)) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT        => await SendQuestDialogAsync(conn, targetObjId, 10002, ct),
                    DialogAction.SELECT_QUEST_REWARD => await SendQuestDialogAsync(conn, targetObjId, 5, ct),
                    _ => false,
                };

            case QuestStatus.REWARD:
                if (!_endNpcs.Contains(targetId)) return false;
                return await SendQuestEndDialogAsync(env, conn, ct);

            default:
                return false;
        }
    }

    private bool AllGroupsComplete(QuestEntry entry)
    {
        foreach (var m in _spawnedMonsters)
            if (m.EndVar > entry.GetVar(m.Var)) return false;
        return true;
    }

    /// <summary>Spawns the monster tied to this spawner object at its own live position (Java's <c>QuestService.addNewSpawn</c>).</summary>
    private bool TrySpawnMonster(QuestEnv env, int spawnerNpcId)
    {
        if (env.Target is not Npc spawner) return false;

        var monster = _spawnedMonsters.FirstOrDefault(m => m.SpawnerObject == spawnerNpcId);
        int monsterNpcId = monster?.NpcIds.FirstOrDefault() ?? 0;
        if (monsterNpcId == 0) return false;

        var monsterTemplate = DataManager.Npcs.GetTemplate(monsterNpcId);
        if (monsterTemplate is null) return false;

        _spawnService.SpawnNpcAt(monsterTemplate, spawner.Position);
        return true;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        foreach (var m in _spawnedMonsters)
        {
            if (!m.NpcIds.Contains(env.TargetId)) continue;
            if (entry.GetVar(m.Var) >= m.EndVar) continue;

            entry.SetVar(m.Var, entry.GetVar(m.Var) + 1);

            if (!AllGroupsComplete(entry))
            {
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }

            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }

        return false;
    }
}
