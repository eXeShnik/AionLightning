// Port of Java data/scripts/system/handlers/quest/greater_stigma/_3932StopTheShulacks.java
// (kecimis). Talk to Miriya (203711) to start; Maloren (204656, var 0->1); back at Miriya, handing
// in the quest_data.xml collect-items flips straight to REWARD (var stays 1); turn in at Miriya
// (any dialog action once REWARD).
// Skip vs Java: onUseSkillEvent (registered for skill 18130) scans every Npc in the world for
// npcId 700516 and kills+deletes the first match — this port has no generic "iterate every
// spawned Npc in the world" API and no Npc AI/controller death/delete hooks, so the side effect
// can't be reproduced. The skill is still registered via RegisterSkillUse for parity; the engine's
// default OnSkillUseAsync no-op means using the skill simply does nothing extra here. The quest's
// own dialog/var progression doesn't depend on this side effect at all, so it stays fully
// completable.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.GreaterStigma;

public sealed class _3932StopTheShulacks : QuestHandlerBase
{
    private const int QuestIdConst = 3932;
    private const int MiriyaNpc    = 203711;
    private const int MalorenNpc   = 204656;
    private const int ShulackSkill = 18130;

    private readonly IItemDao _itemDao;

    public _3932StopTheShulacks(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterSkillUse(ShulackSkill, QuestId);
        engine.RegisterQuestNpc(MiriyaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(MiriyaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MalorenNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != MiriyaNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
            return targetId == MiriyaNpc && await SendQuestEndDialogAsync(env, conn, ct);

        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == MiriyaNpc && var == 1)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 1, reward: true, checkOkId: 5, checkFailId: 2716, ct);
            return false;
        }

        if (targetId == MalorenNpc && var == 0)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            return false;
        }

        return false;
    }
}
