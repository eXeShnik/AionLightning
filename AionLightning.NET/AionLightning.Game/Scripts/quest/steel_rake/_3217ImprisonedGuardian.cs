// Port of Java data/scripts/system/handlers/quest/steel_rake/_3217ImprisonedGuardian.java (vlog).
// The Java source is an unimplemented stub: register() is empty ("TODO Auto-generated method
// stub") and no other logic exists in the class, so there is nothing to port besides the quest id
// itself. quest_data.xml carries a <collect_items> requirement (item 182209095 x3) fed by
// <quest_drop> off npcs 215045/215046/215047, but Java never registered those drops or any
// NPC/kill hook for this quest, so replicating them here would add behavior the original server
// never had.
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.Services;

namespace Quest.SteelRake;

public sealed class _3217ImprisonedGuardian : QuestHandlerBase
{
    private const int QuestIdConst = 3217;

    public _3217ImprisonedGuardian(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        // Java register() is empty — no hooks to wire.
    }
}
