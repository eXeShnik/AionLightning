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
/// Java's kill-count objective (<c>onKillInWorldEvent</c>, incrementing quest var 0 up to
/// <c>amount</c>) is driven exclusively by <c>PvpService.notifyKillQuests</c> — a player-kills-
/// opposing-race-player event, dispatched to every nearby group/alliance member via
/// <c>QuestEngine.onKillInWorld(worldId)</c>. This engine only routes NPC-kill events
/// (<see cref="IQuestHandler.OnKillAsync"/>, wired from <c>QuestService.HandleNpcKillAsync</c>) —
/// there is no PvP-kill event pipeline, and building one is out of scope for this phase (per the
/// task brief: implement the NPC-kill part where one exists, log/skip where it doesn't). Since
/// KillInWorld has no NPC-kill part at all in Java, entries here register only their start/end NPC
/// dialog hooks: a player can accept and see the quest, but its kill counter (quest var 0) never
/// advances, so it can never reach REWARD through normal play — a known, documented limitation.
/// <c>invasion_world</c> (Java's Rift/Vortex-triggered auto-start via <c>onEnterWorldEvent</c>) is
/// parsed for data completeness but not wired, for the same reason MonsterHuntScriptEntry's
/// aggro/invasion fields aren't (no RiftService/VortexService port yet).
/// </remarks>
public sealed class KillInWorldHandler : QuestHandlerBase
{
    private readonly HashSet<int> _startNpcs;
    private readonly HashSet<int> _endNpcs;

    public KillInWorldHandler(KillInWorldScriptEntry data, IDataManager dataManager,
        IQuestDao questDao, QuestRewardService rewardService)
        : base(data.Id, dataManager, questDao, rewardService)
    {
        _startNpcs = data.StartNpcIds;
        _endNpcs   = data.EndNpcIds;
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
                return false; // START: kill counter never advances without PvP-kill event plumbing — see remarks
        }
    }
}
