// DancerAI2 — Java ai/worlds/liveParty/DancerAI2.java. Live-party decoration npc: sets an idle dance
// state per npc-id group on spawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("dancer")]
public sealed class DancerAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        switch (Owner.Template.NpcId)
        {
            case 831633: case 831634: case 831635: case 831637: case 831638: case 831639:
                State = AiState.Idle;
                SubState = AiSubState.None;
                // note: Java also called EmoteManager.emoteStartDancing1(getOwner()); no emote hook is
                // exposed to scripts yet.
                break;
            case 831640: case 831641: case 831642: case 831643:
            case 831644: case 831645: case 831646: case 831647:
                State = AiState.Idle;
                SubState = AiSubState.None;
                // note: EmoteManager.emoteStartDancing2 — see above.
                break;
            case 831648: case 831649: case 831650:
            case 831651: case 831652: case 831653:
                State = AiState.Idle;
                SubState = AiSubState.None;
                // note: EmoteManager.emoteStartDancing3 — see above.
                break;
            case 831617: case 831618:
                State = AiState.Idle;
                SubState = AiSubState.None;
                // note: EmoteManager.emoteStartDancing4 — see above.
                break;
        }
        // note: Java also overrode pollInstance to refuse SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD; no
        // poll-question hook exists on NpcAi2 yet.
    }
}
