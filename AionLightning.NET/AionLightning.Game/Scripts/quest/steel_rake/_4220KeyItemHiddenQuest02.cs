// Port of Java data/scripts/system/handlers/quest/steel_rake/_4220KeyItemHiddenQuest02.java (vlog).
// Asmodian mirror of _3220KeyItemHiddenQuest02. The Java source is an unimplemented stub:
// register() is empty ("TODO Auto-generated method stub") and no other logic exists in the class,
// so there is nothing to port besides the quest id itself. quest_data.xml carries a
// <collect_items> requirement (item 185000079 and others) fed by <quest_drop>, but Java never
// registered those drops or any hook for this "hidden key item" quest.
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.Services;

namespace Quest.SteelRake;

public sealed class _4220KeyItemHiddenQuest02 : QuestHandlerBase
{
    private const int QuestIdConst = 4220;

    public _4220KeyItemHiddenQuest02(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        // Java register() is empty — no hooks to wire.
    }
}
