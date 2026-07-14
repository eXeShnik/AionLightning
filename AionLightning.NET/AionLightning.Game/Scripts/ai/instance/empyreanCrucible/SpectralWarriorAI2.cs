// SpectralWarriorAI2 — Java ai/instance/empyreanCrucible/SpectralWarriorAI2.java. Empyrean Crucible
// add: below 50% HP advances the instance stage and resurrects two fallen allies as upgraded forms.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("spectral_warrior")]
public sealed class SpectralWarriorAI2 : AggressiveNpcAI2
{
    private bool _isDone;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(getOwner().HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage > 50 || _isDone) return;
        _isDone = true;
        // note: Java advanced the instance to StageType.START_STAGE_6_ROUND_5 via InstanceHandler.
        // onChangeStage; instance-handler stage control isn't exposed to scripts yet.
        ScheduleTask(ResurrectAllies, 2000);
    }

    private void ResurrectAllies()
    {
        ReplaceIfAlive(205413, 217576);
        ReplaceIfAlive(205414, 217577);
    }

    private void ReplaceIfAlive(int npcId, int replacementId)
    {
        var npc = GetNpc(npcId);
        if (npc is null || npc.IsAlreadyDead) return;
        Spawn(replacementId, npc.Position.X, npc.Position.Y, npc.Position.Z, (byte)npc.Position.Heading);
        // note: Java deleted the original npc via NpcActions.delete; scripted NPC removal isn't exposed
        // to scripts yet.
    }
}
