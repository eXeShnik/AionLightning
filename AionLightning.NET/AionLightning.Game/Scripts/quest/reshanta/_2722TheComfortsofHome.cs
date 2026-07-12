// Port of Java data/scripts/system/handlers/quest/reshanta/_2722TheComfortsofHome.java.
// Start at 278047; 7-step relay chain (278056 -> 278126 -> 278043 -> 278032 -> 278037 -> 278040 ->
// 278068), each step bumping var0 by one (no var-gate in Java, matching the permissive
// _1718TradingDown style); final step at 278066 gives item 182205654 and flips to REWARD; turn in
// back at 278047.
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

public sealed class _2722TheComfortsofHome : QuestHandlerBase
{
    private const int QuestIdConst = 2722;
    private const int StartNpc     = 278047;
    private const int Relay1 = 278056, Relay2 = 278126, Relay3 = 278043, Relay4 = 278032;
    private const int Relay5 = 278037, Relay6 = 278040, Relay7 = 278068, Relay8 = 278066;
    private const int ItemId = 182205654;

    private readonly IItemDao _itemDao;

    public _2722TheComfortsofHome(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Relay1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay2).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay3).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay4).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay5).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay6).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay7).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay8).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status != QuestStatus.START)
        {
            if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
                return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }

        int var = entry.GetVar(0);
        (int Npc, DialogAction Setpro, int SelectPage)[] steps =
        [
            (Relay1, DialogAction.SETPRO1, 1011),
            (Relay2, DialogAction.SETPRO2, 1352),
            (Relay3, DialogAction.SETPRO3, 1693),
            (Relay4, DialogAction.SETPRO4, 2034),
            (Relay5, DialogAction.SETPRO5, 2375),
            (Relay6, DialogAction.SETPRO6, 2716),
            (Relay7, DialogAction.SETPRO7, 3057),
        ];

        foreach (var (npc, setpro, selectPage) in steps)
        {
            if (targetId != npc) continue;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, selectPage, ct);
            if (dialog == setpro)
            {
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return false;
        }

        if (targetId == Relay8)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
            if (dialog == DialogAction.SET_SUCCEED)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct)) return true;
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
        }

        return false;
    }
}
