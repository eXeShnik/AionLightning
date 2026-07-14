// MeropsAI2 — Java ai/instance/rentusBase/MeropsAI2.java. Post-boss regroup npc shared by 4 npc ids
// (799671-799674): triggers a scripted spawn/shout sequence once a player approaches, then hands off to
// a boss-specific follow-up (rescue sequence or the Vasharti revival scene).
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("merops")]
public sealed class MeropsAI2 : GeneralNpcAI2
{
    private bool _startedEvent;

    public override void OnCreatureMoved(Creature creature)
    {
        if (creature is not Player player) return;
        if (Owner.Position.DistanceTo(player.Position) > 31) return;
        if (_startedEvent) return;
        _startedEvent = true;
        if (Owner.Template.NpcId != 799674)
        {
            UseSkill(19358);
            UseSkill(19922);
        }
        StartEvent();
    }

    private void StartEvent()
    {
        if (Owner.IsAlreadyDead) return;
        switch (Owner.Template.NpcId)
        {
            case 799671:
                SendMsg(1500421);
                SendMsg(1500422);
                SendMsg(1500423);
                break;
            case 799672:
                SendMsg(1500426);
                SendMsg(1500427);
                SendMsg(1500428);
                break;
        }
        ScheduleTask(ContinueEvent, 9000);
    }

    private void ContinueEvent()
    {
        if (Owner.IsAlreadyDead) return;
        if (Owner.Template.NpcId != 799674) UseSkill(19358);
        switch (Owner.Template.NpcId)
        {
            case 799671:
                Spawn(282546, 751.380f, 625.360f, 157f, 10);
                Spawn(282546, 748.950f, 645.180f, 157f, 114);
                ScheduleTask(() =>
                {
                    if (Owner.IsAlreadyDead) return;
                    Spawn(282465, 751.366f, 647.573f, 155.681f);
                    Spawn(282465, 749.39f, 627.755f, 155.691f);
                    Spawn(282543, 749.39f, 627.755f, 155.691f);
                    Spawn(282543, 751.366f, 647.573f, 155.681f);
                }, 1500);
                break;
            case 799672:
                SendMsg(1500429);
                Spawn(282547, 255.980f, 684.432f, 170f, 7);
                Spawn(282547, 270.590f, 672.190f, 170f, 22);
                ScheduleTask(() =>
                {
                    if (Owner.IsAlreadyDead) return;
                    Spawn(282465, 256.308f, 684.923f, 168.356f);
                    Spawn(282465, 270.427f, 672.150f, 169.167f);
                    Spawn(282544, 256.308f, 684.923f, 168.356f);
                    Spawn(282544, 270.427f, 672.150f, 169.167f);
                }, 1500);
                break;
            case 799673:
                Spawn(282548, 160.981f, 310.663f, 252.031f, 85);
                ScheduleTask(FinishRescueSequence, 1500);
                break;
            default:
                StartVashartiSequence();
                break;
        }
    }

    private void FinishRescueSequence()
    {
        if (Owner.IsAlreadyDead) return;
        Spawn(282545, 160.907f, 309.474f, 252.202f);
        Spawn(282545, 162.361f, 312.186f, 252.032f);
        Spawn(217317, 160.489f, 308.799f, 252.032f);
        Spawn(282465, 160.907f, 309.474f, 252.202f);
        Spawn(282465, 162.361f, 312.186f, 252.032f);
        Spawn(282465, 160.489f, 308.799f, 252.032f, 85);
        SendMsg(1500432);
        // note: Java also set a walker route (30028000001) here — the walker system isn't exposed to
        // scripts yet, but the state transition below is faithful.
        State = AiState.Walking;
        Think();
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) return;
            // note: Java broadcast a START_EMOTE2 packet here (PacketSendUtility) — not exposed to
            // scripts.
            ScheduleTask(() =>
            {
                if (Owner.IsAlreadyDead) return;
                // note: Java reopened instance door 70 and then deleted itself (AI2Actions.deleteOwner) —
                // instance doors and NPC delete aren't exposed to scripts yet.
            }, 11000);
        }, 6000);
    }

    private void StartVashartiSequence()
    {
        var vasharti = GetNpc(218614);
        if (vasharti is null) return;
        SendMsg(1500435);
        SendMsg(1500436);
        // note: Java shouted three lines through `vasharti` directly (NpcShoutsService against that npc,
        // not this one) and cast skill 19907 on it — cross-npc shouts/skill casts aren't exposed to
        // scripts yet.
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead || vasharti.IsAlreadyDead) return;
            ScheduleTask(() =>
            {
                if (vasharti.IsAlreadyDead) return;
                SendMsg(1500437);
                // note: Java deleted `vasharti` here, then spawned npc 217313 4s later — scripted NPC
                // delete isn't exposed to scripts yet.
                Spawn(217313, 188.17f, 414.06f, 260.75488f, 86);
            }, 2000);
        }, 14000);
    }
}
