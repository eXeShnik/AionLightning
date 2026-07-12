// Port of Java data/scripts/system/handlers/quest/reshanta/_1798JakurerksShotattheBigTime.java
// (Hilgert). Start/turn-in at 279007; 9-step relay chain (263568 -> 263266 -> 264768 -> 271053 ->
// 266553 -> 270151 -> 269251 -> 268051 -> 260235), each bumping var0 by one, the last flipping to
// REWARD via SET_SUCCEED (no var-gate in Java, matching the permissive _1718TradingDown style).
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

namespace Quest.Reshanta;

public sealed class _1798JakurerksShotattheBigTime : QuestHandlerBase
{
    private const int QuestIdConst = 1798;
    private const int StartNpc     = 279007;
    private const int Relay1 = 263568, Relay2 = 263266, Relay3 = 264768, Relay4 = 271053, Relay5 = 266553;
    private const int Relay6 = 270151, Relay7 = 269251, Relay8 = 268051, Relay9 = 260235;

    public _1798JakurerksShotattheBigTime(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay2).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay3).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay4).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay5).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay6).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay7).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay8).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay9).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD)
                return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }

        if (entry is not { Status: QuestStatus.START }) return false;
        int var = entry.GetVar(0);

        (int Npc, DialogAction Setpro, int SelectPage, bool Finish)[] steps =
        [
            (Relay1, DialogAction.SETPRO1, 1011, false),
            (Relay2, DialogAction.SETPRO2, 1352, false),
            (Relay3, DialogAction.SETPRO3, 1693, false),
            (Relay4, DialogAction.SETPRO4, 2035, false),
            (Relay5, DialogAction.SETPRO5, 2375, false),
            (Relay6, DialogAction.SETPRO6, 2716, false),
            (Relay7, DialogAction.SETPRO7, 3057, false),
            (Relay8, DialogAction.SETPRO8, 3398, false),
            (Relay9, DialogAction.SET_SUCCEED, 3740, true),
        ];

        foreach (var (npc, action, selectPage, finish) in steps)
        {
            if (targetId != npc) continue;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, selectPage, ct);
            if (dialog == action)
            {
                entry.SetVar(0, var + 1);
                if (finish) entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return false;
        }

        return false;
    }
}
