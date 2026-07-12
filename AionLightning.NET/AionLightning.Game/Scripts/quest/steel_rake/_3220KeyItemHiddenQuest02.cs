// Port of Java data/scripts/system/handlers/quest/steel_rake/_3220KeyItemHiddenQuest02.java (vlog).
// The Java source is an unimplemented stub: register() is empty ("TODO Auto-generated method
// stub") and no other logic exists in the class, so there is nothing to port besides the quest id
// itself. quest_data.xml carries a <collect_items> requirement (items 185000055/185000050/185000073)
// fed by <quest_drop> off npcs 215058/215066/215411, but Java never registered those drops or any
// hook for this "hidden key item" quest, so replicating them here would add behavior the original
// server never had.
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.Services;

namespace Quest.SteelRake;

public sealed class _3220KeyItemHiddenQuest02 : QuestHandlerBase
{
    private const int QuestIdConst = 3220;

    public _3220KeyItemHiddenQuest02(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        // Java register() is empty — no hooks to wire.
    }
}
