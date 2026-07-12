// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41526PyroMania.java (Cheatkiller).
// Talk to 205941 to start (grants item 182212587); harvesting skill 10379 spawns a lootable remains
// object (701235) which drops item 182212529 - collect 5 of them and hand them in at 205941 to flip
// to REWARD; turn in (removes the start item).
// Skip vs Java: onUseSkillEvent also requires the player's current target to be an npc named
// "pyroclast", and calls npc.getController().die() to remove it before spawning the remains at its
// exact position - OnSkillUseAsync in this port carries no target information (matching the
// precedent already established by sarpan._41304BringOnTheHungryPagatis /
// greater_stigma._3932StopTheShulacks) and there is no NPC death/despawn API ported yet, so every
// cast of skill 10379 while the quest is active spawns the remains at the player's own position
// instead, and the original target npc (if any) is left alone. Doesn't block completability; only
// loosens the "must harvest a pyroclast" restriction and leaves the harvested mob visually
// unremoved.
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

namespace Quest.Tiamaranta;

public sealed class _41526PyroMania : QuestHandlerBase
{
    private const int QuestIdConst  = 41526;
    private const int StartNpc      = 205941;
    private const int RemainsNpc    = 701235;
    private const int HarvestSkill = 10379;
    private const int StartItemId  = 182212587;
    private const int AshItemId    = 182212529;

    private readonly IItemDao _itemDao;

    public _41526PyroMania(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterSkillUse(HarvestSkill, QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RemainsNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                await GiveQuestItemAsync(player, conn, _itemDao, StartItemId, 1, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == RemainsNpc) return true; // loot passthrough

            if (targetId == StartNpc)
            {
                bool hasFive = (player.Inventory.FindByItemId(AshItemId)?.Count ?? 0) >= 5;
                if (dialog == DialogAction.QUEST_SELECT && hasFive)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog is DialogAction.QUEST_SELECT or DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, reward: true, checkOkId: 5, checkFailId: 10001, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, StartItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnSkillUseAsync(Player player, int skillId, GsClientConnection conn, CancellationToken ct)
    {
        if (skillId != HarvestSkill) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        var pos = player.Position;
        SpawnQuestNpc(pos.WorldId, pos.InstanceId, RemainsNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
        return true;
    }
}
