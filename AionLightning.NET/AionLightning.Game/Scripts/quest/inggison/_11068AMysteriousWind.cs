// Port of Java data/scripts/system/handlers/quest/inggison/_11068AMysteriousWind.java.
// Talk to 799025 to start (no item); relay at 799026 (var 0) gives item 182206858 (var only
// advances if the give succeeds, faithfully matching Java's guarded increment) and shows the
// generic select page; turn in back at 799025 removes the item and flips to reward.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Inggison;

public sealed class _11068AMysteriousWind : QuestHandlerBase
{
    private const int QuestIdConst = 11068;
    private const int StartNpc     = 799025;
    private const int RelayNpc     = 799026;
    private const int TokenItem    = 182206858;

    private readonly IItemDao _itemDao;

    public _11068AMysteriousWind(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc && (entry is null || entry.Status == QuestStatus.NONE))
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry is null) return false;

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        else if (entry.Status != QuestStatus.START)
        {
            return false;
        }

        if (targetId == RelayNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1 && entry.GetVar(0) == 0)
            {
                if (await GiveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct))
                    entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
        }
        else if (targetId == StartNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD && entry.GetVar(0) == 1)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct);
                entry.SetVar(0, 2);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
        }
        return false;
    }
}
