// SuramaTheTraitorAI2 — Java ai/instance/tiamatStrongHold/SuramaTheTraitorAI2.java. Story NPC:
// walks to Raksha, runs a shout/skill cutscene, then flips Raksha to attackable.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("suramathetraitor")]
public sealed class SuramaTheTraitorAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        MoveToRaksha();
    }

    public override void OnDied()
    {
        base.OnDied();
        SendMsg(390845);
    }

    private void MoveToRaksha()
    {
        // note: Java walked the owner to map point (651, 1319, 487) via MoveController/WalkManager and
        // broadcast a START_EMOTE2 SM_EMOTION — movement/emote plumbing isn't exposed to scripts yet.
        ScheduleTask(StartDialog, 10000);
    }

    private void StartDialog()
    {
        // Raksha (219356) is the boss triggered by this cutscene.
        SendMsg(390841);
        SendMsg(390842);
        // note: Java also had Raksha (a separate NPC) shout line 390843 — NpcShoutsService only fires
        // on this AI's own owner via SendMsg, cross-NPC shouts aren't exposed to scripts yet.
        ScheduleTask(() =>
        {
            // note: Java targeted Raksha on this owner and cast skill 20952 via SkillEngine, then
            // flipped Raksha's NPC type to ATTACKABLE and broadcast SM_CUSTOM_SETTINGS to nearby
            // players. Cross-NPC skill casting and runtime NPC-type changes aren't exposed to scripts
            // yet.
        }, 8000);
    }
}
