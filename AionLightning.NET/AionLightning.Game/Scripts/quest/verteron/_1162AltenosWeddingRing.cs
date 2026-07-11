// Port of Java data/scripts/system/handlers/quest/verteron/_1162AltenosWeddingRing.java
// (Balthazar). Talk to Altenos (203095) to start; use the item stack (700005) to receive the
// wedding ring (var 0->1); return to Altenos/Sulinda (203093/203095) to turn in.
// Skip vs Java: the SM_EMOTION(DIE) broadcast on the target the player was targeting when using
// the item stack is omitted — cosmetic only (Java's own comment on it reads "wtf ?").
using System.Linq;
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

namespace Quest.Verteron;

public sealed class _1162AltenosWeddingRing : QuestHandlerBase
{
    private const int QuestIdConst  = 1162;
    private const int AltenosNpc    = 203095;
    private const int SulindaNpc    = 203093;
    private const int ItemStackObj  = 700005;
    private const int RingItemId    = 182200563;

    private readonly IItemDao _itemDao;

    public _1162AltenosWeddingRing(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(AltenosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(AltenosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SulindaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ItemStackObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        var entry = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null)
        {
            if (targetId == AltenosNpc)
            {
                if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == ItemStackObj)
            {
                if (DialogActionLookup.FromId(env.DialogId) != DialogAction.USE_OBJECT) return false;

                if (player.Inventory.FindByItemId(RingItemId) is null)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, RingItemId, 1, ct)) return true;
                }
                entry.SetVar(0, entry.GetVar(0) + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }

            if (targetId == SulindaNpc || targetId == AltenosNpc)
            {
                if (entry.GetVar(0) == 1)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, RingItemId, 1, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == AltenosNpc)
        {
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
