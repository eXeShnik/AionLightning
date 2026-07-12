// Port of Java data/scripts/system/handlers/quest/pandaemonium/_4920MakingTheActivatedSurkana.java.
// Accept at Chopirunerk (798358) gives the Inactivated Surkana (182207100); using the Balaur
// Material Converter (730212) while carrying it swaps it for the Activated Surkana (182207101)
// and flips straight to REWARD; turn in back at Chopirunerk. Java registers 182207100/182207101
// as quest items but never overrides onItemUseEvent, so those registrations are inert (ported
// faithfully — the items are only ever consumed via UseQuestObjectAsync at the converter).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Pandaemonium;

public sealed class _4920MakingTheActivatedSurkana : QuestHandlerBase
{
    private const int QuestIdConst = 4920;
    private const int ChopirunerkNpc = 798358;
    private const int ConverterObj = 730212;
    private const int InactivatedSurkana = 182207100;
    private const int ActivatedSurkana = 182207101;

    private readonly IItemDao _itemDao;

    public _4920MakingTheActivatedSurkana(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ChopirunerkNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ChopirunerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ConverterObj).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(InactivatedSurkana, QuestId);
        engine.RegisterQuestItem(ActivatedSurkana, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != ChopirunerkNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_1)
                await GiveQuestItemAsync(player, conn, _itemDao, InactivatedSurkana, 1, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != ConverterObj) return false;
            if (dialog != DialogAction.USE_OBJECT || entry.GetVar(0) != 0) return false;
            if (player.Inventory.FindByItemId(InactivatedSurkana) is null) return false;

            return await UseQuestObjectAsync(env, conn, step: 0, nextStep: 1, reward: true, varNum: 0,
                addItemId: ActivatedSurkana, addItemCount: 1,
                removeItemId: InactivatedSurkana, removeItemCount: 1, movieId: 0, dieObject: false, _itemDao, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ChopirunerkNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
