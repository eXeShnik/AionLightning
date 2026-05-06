namespace AionLightning.Game.Model;

/// <summary>
/// Bitmask of active CC states. Values match Java AbnormalState enum IDs exactly
/// (com.aionemu.gameserver.skillengine.effect.AbnormalState) so the mask can be
/// written directly to SM_ABNORMAL_EFFECT without conversion.
/// CANT_ATTACK_STATE = Spin | Sleep | Stun | Stumble | Stagger | OpenAerial | Paralyze | Fear | CannotMove.
/// CANT_MOVE_STATE   = Spin | Root  | Sleep | Stumble | Stun | Stagger | OpenAerial | Paralyze | Bind | CannotMove (+ Fear via isUnderFear).
/// </summary>
[Flags]
public enum AbnormalCcFlags : long
{
    None       = 0,
    Paralyze   = 4,
    Sleep      = 8,
    Root       = 16,
    Blind      = 32,      // Java BLIND = 32 (was 1 — wrong)
    Silence    = 256,
    Fear       = 512,
    Curse      = 1024,    // Java CURSE = 1024 (was 131072 — wrong)
    Stun       = 4096,
    Stumble    = 16384,
    Stagger    = 32768,
    OpenAerial = 65536,
    Snare      = 131072,  // Java SNARE = 131072 (new)
    Slow       = 262144,  // Java SLOW  = 262144 (new)
    Spin       = 524288,
    Bind       = 1048576, // Java BIND  = 1048576 (new; movement-lock, still allows attacking)
    CannotMove = 4194304,
    NoFly      = 8388608, // Java NOFLY = 8388608 (new)

    CantAttack = Spin | Sleep | Stun | Stumble | Stagger | OpenAerial | Paralyze | Fear | CannotMove,
    CantMove   = Spin | Root  | Sleep | Stumble | Stun | Stagger | OpenAerial | Paralyze | Fear | Bind | CannotMove,
}
