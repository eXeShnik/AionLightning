// Port of Java data/scripts/system/handlers/quest/inggison/_11077AWeaponOfWorth.java.
// Talk to Outremus (798926) to start — approaching him before accepting always grants 182214016
// (Java's inverted `if (!giveQuestItem(...)) updateQuestStatus(env)` failure branch is a no-op since
// no quest entry exists yet at that point, so it is dropped rather than ported literally). Brontes
// (799028, var0->1) -> Pilipides (798918, var1->2) -> Drenia (798903, var2->reward, same npc turn-in).
// Java's outer npc switch is missing `break`s (falls from Brontes's case into Pilipides's into
// Drenia's when the inner dialog switch doesn't match) — harmless in practice since the client only
// ever sends dialog ids matching the NPC's own currently-open dialog tree, so this is ported as
// independent if-checks per npc instead of literally reproducing the fallthrough.
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

namespace Quest.Inggison;

public sealed class _11077AWeaponOfWorth : QuestHandlerBase
{
    private const int QuestIdConst = 11077;
    private const int Outremus  = 798926;
    private const int Brontes   = 799028;
    private const int Pilipides = 798918;
    private const int Drenia    = 798903;
    private const int PrereqItem = 182214016;

    private readonly IItemDao _itemDao;

    public _11077AWeaponOfWorth(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Outremus).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Outremus).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Brontes).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Pilipides).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Drenia).OnTalk.Add(QuestId);
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
            if (targetId != Outremus) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            await GiveQuestItemAsync(player, conn, _itemDao, PrereqItem, 1, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Brontes)
            {
                if (dialog is DialogAction.QUEST_SELECT or DialogAction.SELECT_ACTION_1353)
                    return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == Pilipides)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SELECT_ACTION_1694)
                    return await SendQuestDialogAsync(conn, targetObjId, 1694, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == Drenia)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, reward: true, sameNpc: true, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == Drenia)
        {
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
