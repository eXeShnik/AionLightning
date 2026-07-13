// Port of Java data/scripts/system/handlers/quest/kromedes_trial/_28604RecoveringRotan.java (Rolandas).
// Asmodian counterpart of _18604: auto-starts on entering GRAND_CAVERN_300230000; using the Grave
// Robber's Corpse (700961, USE_OBJECT) while START flips to REWARD and finishes; otherwise it hands
// out lore item 164000141 (dialog 1012) or reports it already held (dialog 27).
// Skip vs Java: onCanAct has no hook in this port — the corpse is registered for OnTalk so its dialog
// is dispatched regardless, so the gate is a no-op here.
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

namespace Quest.KromedesTrial;

public sealed class _28604RecoveringRotan : QuestHandlerBase
{
    private const int QuestIdConst     = 28604;
    private const int CorpseObj        = 700961; // Grave Robber's Corpse
    private const int LoreItem         = 164000141;
    private const string EnterZoneName = "GRAND_CAVERN_300230000";

    private readonly IItemDao _itemDao;

    public _28604RecoveringRotan(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        RegisterOnEnterZone(engine, EnterZoneName);
        engine.RegisterQuestNpc(CorpseObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != EnterZoneName) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not null && entry.Status != QuestStatus.NONE) return false;

        return await StartMissionAsync(conn, env.Player, QuestStatus.START, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        if (env.TargetId != CorpseObj) return false;
        if (DialogActionLookup.FromId(env.DialogId) != DialogAction.USE_OBJECT) return false;

        if (entry.Status == QuestStatus.START)
        {
            entry.Status = QuestStatus.REWARD;
            return await FinishQuestAsync(conn, player, 0, ct);
        }

        var held = player.Inventory.FindByItemId(LoreItem);
        if (held is not null && held.Count >= 1)
            return await SendQuestDialogAsync(conn, targetObjId, 27, ct);

        await GiveQuestItemAsync(player, conn, _itemDao, LoreItem, 1, ct);
        return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
    }
}
