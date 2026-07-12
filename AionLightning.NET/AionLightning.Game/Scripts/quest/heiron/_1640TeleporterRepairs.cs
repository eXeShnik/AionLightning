// Port of Java data/scripts/system/handlers/quest/heiron/_1640TeleporterRepairs.java.
// A single-NPC (730033) repair quest: SETPRO1 starts it directly (no accept-confirm dialog); using
// the object while holding the repair kit (182201790) flips to REWARD; turning in removes the kit
// and grants the reward.
// Skip vs Java: the sit-emote broadcast + 3s "still targeting" delay before flipping to REWARD, the
// manual exp/kinah math (this port grants via the standard reward service instead), and the
// delayed teleport back to Heiron on COMPLETE aren't ported — no animation-delay or teleport
// infra exists yet; the quest still starts, completes, and pays out correctly.
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

namespace Quest.Heiron;

public sealed class _1640TeleporterRepairs : QuestHandlerBase
{
    private const int QuestIdConst = 1640;
    private const int TeleporterNpc = 730033;
    private const int RepairKitItem = 182201790;

    private readonly IItemDao _itemDao;

    public _1640TeleporterRepairs(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TeleporterNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TeleporterNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (env.TargetId != TeleporterNpc) return false;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry  = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (dialog == DialogAction.USE_OBJECT && player.Inventory.FindByItemId(RepairKitItem) is { Count: >= 1 })
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, RepairKitItem, 1, ct);
            return await FinishQuestAsync(conn, player, 0, ct);
        }
        return false;
    }
}
