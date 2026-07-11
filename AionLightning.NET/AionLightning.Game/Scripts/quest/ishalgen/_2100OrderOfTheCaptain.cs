// Port of Java data/scripts/system/handlers/quest/ishalgen/_2100OrderOfTheCaptain.java.
// Ishalgen campaign opener (Asmodian mirror of Poeta's Kalio's Call): auto-starts on entering
// Aldelle Village, talk to Ulgorn (203516) → REWARD, turn-in pokes the 2001-2007 chain.
// Skip vs Java: zone-polygon start (ALDELLE_VILLAGE_220010000) → OnEnterWorld gated to
// worldId 220010000 (Ishalgen), same net effect for a new character.
using System.Linq;
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

namespace Quest.Ishalgen;

public sealed class _2100OrderOfTheCaptain : QuestHandlerBase
{
    private const int QuestIdConst    = 2100;
    private const int UlgornNpc       = 203516;
    private const int IshalgenWorldId = 220010000;

    private static readonly int[] _chainQuestIds = [2001, 2002, 2003, 2004, 2005, 2006, 2007];

    private QuestEngine? _engine;

    public _2100OrderOfTheCaptain(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterQuestNpc(UlgornNpc).OnTalk.Add(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Position.WorldId != IshalgenWorldId) return false;
        if (player.Quests.Get(QuestId) is not null) return false;
        return await StartMissionAsync(conn, player, QuestStatus.START, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || env.TargetId != UlgornNpc) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.DialogId == (int)DialogAction.SELECTED_QUEST_NOREWARD && _engine is not null)
            {
                foreach (int id in _chainQuestIds)
                    await _engine.OnZoneMissionEndAsync(new QuestEnv(env.Target, player, id, env.DialogId), conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
