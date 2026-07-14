// KexkraPrototypeAI2 — Java ai/instance/esoterrace/KexkraPrototypeAI2.java. Boss that plays a
// cutscene and spawns its next phase once below 75% HP.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("kexkraprototype")]
public sealed class KexkraPrototypeAI2 : AggressiveNpcAI2
{
    private bool _eventStarted;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (Owner.HpPercentage <= 75 && !_eventStarted)
        {
            _eventStarted = true;
            // note: Java played movie 472 (SM_PLAY_MOVIE) for every online, living player in the
            // owner's known-list here; KnownList enumeration and PacketSendUtility aren't exposed to
            // the script layer yet.
            Spawn(217206, 1320.639282f, 1171.063354f, 51.494003f);
            // note: Java also deleted the owner (AI2Actions.deleteOwner) after spawning; no scripted
            // despawn API exists yet.
        }
    }
}
