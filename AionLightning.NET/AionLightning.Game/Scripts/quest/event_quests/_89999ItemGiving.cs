// Port of Java data/scripts/system/handlers/quest/event_quests/_89999ItemGiving.java.
// Event NPCs hand out a single juice/cake item on request (no quest state — a pure item-giving dialog).
// note: Java reads the given item ids from EventsConfig.EVENT_GIVEJUICE / EVENT_GIVECAKE (server config,
//   gameserver.events.givejuice / givecake). Those config keys are not ported, so the Java default values
//   (juice 160009017, cake 160010073) are hardcoded here.
// note: Java sends SM_DIALOG_WINDOW with questId 0 (these dialogs aren't quest-tracked); replicated by
//   sending the packet directly rather than via SendQuestDialogAsync (which would stamp this quest id).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.EventQuests;

public sealed class _89999ItemGiving : QuestHandlerBase
{
    private const int QuestIdConst   = 89999;
    private const int LaylinNpc      = 799702; // Elyos juice
    private const int RonyaNpc       = 799703; // Asmodian juice
    private const int BriosNpc       = 798414; // Elyos cake
    private const int BothenNpc      = 798416; // Asmodian cake
    private const int EventGiveJuice = 160009017; // EventsConfig.EVENT_GIVEJUICE default
    private const int EventGiveCake  = 160010073; // EventsConfig.EVENT_GIVECAKE default

    private readonly IItemDao _itemDao;

    public _89999ItemGiving(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(LaylinNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RonyaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BriosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BothenNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;

        int itemId = env.TargetId switch
        {
            RonyaNpc or LaylinNpc => EventGiveJuice,
            BothenNpc or BriosNpc => EventGiveCake,
            _                     => 0,
        };

        if (itemId == 0) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;

        switch (DialogActionLookup.FromId(env.DialogId))
        {
            case DialogAction.USE_OBJECT:
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 1011, 0), ct);
                return true;

            case DialogAction.SELECT_ACTION_1012:
            {
                long held = player.Inventory.FindByItemId(itemId)?.Count ?? 0;
                if (held > 0)
                {
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 1097, 0), ct);
                    return true;
                }
                if (await GiveQuestItemAsync(player, conn, _itemDao, itemId, 1, ct))
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 1012, 0), ct);
                return true;
            }

            default:
                return false;
        }
    }
}
