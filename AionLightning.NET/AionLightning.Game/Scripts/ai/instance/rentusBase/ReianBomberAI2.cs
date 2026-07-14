// ReianBomberAI2 — Java ai/instance/rentusBase/ReianBomberAI2.java. Escort npc: walks a fixed route to
// the boss room, then cycles between three waypoints spawning a helper npc at each stop.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("reian_bomber")]
public sealed class ReianBomberAI2 : GeneralNpcAI2
{
    private int _position = 1;

    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java started a walker route (30028000024) and broadcast a START_EMOTE2 "running" packet
        // here — WalkManager/PacketSendUtility have no equivalent at the script layer.
    }

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        // note: Java tracked the walker's current route point (MoveController.getCurrentPoint(), not
        // exposed to scripts) to detect arrival at the boss room (point 7) before starting the help-event
        // loop below; that gating can't be reproduced faithfully, so the loop runs unconditionally on
        // every arrival as a best effort.
        ScheduleTask(HelpEvent, 8000);
    }

    private void HelpEvent()
    {
        if (Owner.IsAlreadyDead) return;
        UseSkill(19374);
        switch (_position)
        {
            case 1:
                Help(359.763f, 585.597f, 145.525f);
                _position = 2;
                break;
            case 2:
                Help(346.086f, 597.062f, 146.119f);
                _position = 3;
                break;
            case 3:
                Help(362.143f, 604.723f, 146.125f);
                _position = 1;
                break;
        }
        // note: Java also transitioned into a random-walk sub-state and issued a move-to-point order to
        // the next waypoint — MoveController isn't exposed to scripts yet.
    }

    private void Help(float x, float y, float z)
    {
        // note: Java first deleted any 282530 "collapsed" npc standing at (x, y) and skipped spawning if a
        // 282387 helper already stood there (WorldMapInstance.getNpcs by-coordinate scan) — that scan
        // isn't exposed to scripts, so this always (re)spawns a helper.
        var npc = Spawn(282387, x, y, z);
        if (npc is not null) UseSkill(19731);
    }
}
