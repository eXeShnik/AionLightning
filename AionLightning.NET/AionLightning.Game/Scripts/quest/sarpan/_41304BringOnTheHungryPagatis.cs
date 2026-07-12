// Port of Java data/scripts/system/handlers/quest/sarpan/_41304BringOnTheHungryPagatis.java
// (Cheatkiller). Talk to 205794 to start (gives item 182213111); killing either Pagatis mob
// (218157/218156) spawns a feeding trough (701168) at the kill position; casting skill 10390 up to
// 10 times advances var 0, the 10th flips to REWARD; turn in at 205794 (removes the item first).
// Skip vs Java: onUseSkillEvent also requires the player's current target to be the spawned trough
// (npc template 701168) - OnSkillUseAsync in this port carries no target information (Java's own
// CM_CASTSPELL wiring only forwards player+skillId, matching the precedent already established by
// quest.greater_stigma._3932StopTheShulacks), so every cast of skill 10390 while the quest is active
// advances the var regardless of target. Doesn't block completability; only loosens the original
// "must actually use it on the trough" restriction.
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

namespace Quest.Sarpan;

public sealed class _41304BringOnTheHungryPagatis : QuestHandlerBase
{
    private const int QuestIdConst = 41304;
    private const int StartNpc     = 205794;
    private const int Skill        = 10390;
    private const int Pagatis1Id   = 218157;
    private const int Pagatis2Id   = 218156;
    private const int TroughNpc    = 701168;
    private const int PouchItemId  = 182213111;

    private readonly IItemDao _itemDao;

    public _41304BringOnTheHungryPagatis(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterSkillUse(Skill, QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Pagatis1Id).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Pagatis2Id).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await StartWithItemAsync(player, conn, targetObjId, dialog, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, PouchItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;
        if ((targetId == Pagatis1Id || targetId == Pagatis2Id) && env.Target is not null)
        {
            var pos = env.Target.Position;
            SpawnQuestNpc(pos.WorldId, pos.InstanceId, TroughNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
            return true;
        }

        return false;
    }

    public override async ValueTask<bool> OnSkillUseAsync(Player player, int skillId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var < 9)
        {
            entry.SetVar(0, var + 1);
        }
        else
        {
            entry.Status = QuestStatus.REWARD;
        }
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    private async ValueTask<bool> StartWithItemAsync(Player player, GsClientConnection conn, int targetObjId, DialogAction dialog, CancellationToken ct)
    {
        switch (dialog)
        {
            case DialogAction.QUEST_ACCEPT:
            case DialogAction.QUEST_ACCEPT_1:
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await GiveQuestItemAsync(player, conn, _itemDao, PouchItemId, 1, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
            case DialogAction.QUEST_ACCEPT_SIMPLE:
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await GiveQuestItemAsync(player, conn, _itemDao, PouchItemId, 1, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            case DialogAction.QUEST_REFUSE_1:
            case DialogAction.QUEST_REFUSE_2:
                return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
            case DialogAction.QUEST_REFUSE_SIMPLE:
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            default:
                return false;
        }
    }
}
