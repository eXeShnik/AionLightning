// Port of Java data/scripts/system/handlers/quest/inggison/_11143BabyShulackJourney.java.
// Item-use start (182206866, obtained unconditionally by talking to 700909); using it opens the
// accept dialog (targetId 0). Relay chain: 798985 (var 0->1), 798984 (var 1->2), 798976 (var 2,
// self-sets var 3 like Java before flipping to reward — same redundant-gate idiom as
// _11212BalaurRecords.cs). Turn in at 798948 removes the item on any non-USE_OBJECT dialog.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Inggison;

public sealed class _11143BabyShulackJourney : QuestHandlerBase
{
    private const int QuestIdConst = 11143;
    private const int GiverNpc     = 700909;
    private const int Relay1Npc    = 798985;
    private const int Relay2Npc    = 798984;
    private const int Relay3Npc    = 798976;
    private const int TurnInNpc    = 798948;
    private const int JourneyItem  = 182206866;

    private readonly IItemDao _itemDao;

    public _11143BabyShulackJourney(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(JourneyItem, QuestId);
        engine.RegisterQuestNpc(GiverNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay1Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay2Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay3Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != JourneyItem) return false;
        if (player.Quests.Get(QuestId) is not null) return false;
        await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == 0 && env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, 0, ct);
            }
            if (targetId == GiverNpc)
                return await GiveQuestItemAsync(player, conn, _itemDao, JourneyItem, 1, ct);
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Relay1Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            else if (targetId == Relay2Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            }
            else if (targetId == Relay3Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    entry.SetVar(0, 3);
                    return await DefaultCloseDialogAsync(env, conn, 3, 3, reward: true, sameNpc: false, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, JourneyItem, 1, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
