// QuestItemNpcAI2 — Java ai/quests/QuestItemNpcAI2.java. "Use item" NPC that opens a quest-drop
// dialog and registers the interacting player (or their group/alliance) for a drop roll.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("quest_use_item")]
public sealed class QuestItemNpcAI2 : ActionItemNpcAI2
{
    private readonly List<Player> _registeredPlayers = new();

    public override void OnDialogStart(Player player)
    {
        // note: Java gated this on QuestEngine.onCanAct(..., ACTION_ITEM_USE) before delegating to the
        // base use-item flow — QuestEngine isn't reachable from the script layer, so the gate is dropped.
        base.OnDialogStart(player);
    }

    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java resolved a quest-drop dialog via AI2Actions.selectDialog (falling back to
        // SM_DIALOG_WINDOW(SELECT_ACTION_1011) for dialog NPCs on failure), then registered the player
        // (or their group/alliance, via QuestService.getEachDropMembersGroup/Alliance) for a drop roll
        // and requested the drop-list window via DropService. None of AI2Actions, QuestService,
        // DropService or the drop-window packet flow are reachable from the script layer.
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        _registeredPlayers.Clear();
    }

    public override void OnCreatureSee(Creature creature)
    {
        // note: Java delegated to CreatureEventHandler.onCreatureSee — framework plumbing with no C#
        // equivalent.
    }

    public override void OnCreatureMoved(Creature creature)
    {
        // note: Java delegated to CreatureEventHandler.onCreatureMoved — framework plumbing with no C#
        // equivalent.
    }
}
