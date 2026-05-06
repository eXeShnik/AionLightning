namespace AionLightning.Game.Model;

/// <summary>
/// Bitmask of active CC states. Values match Java AbnormalState enum IDs
/// (com.aionemu.gameserver.skillengine.effect.AbnormalState).
/// CANT_ATTACK_STATE = Spin | Sleep | Stun | Stumble | Stagger | OpenAerial | Paralyze | Fear | CannotMove.
/// CANT_MOVE_STATE   = Spin | Root  | Sleep | Stumble | Stun | Stagger | OpenAerial | Paralyze | CannotMove (+ Fear via isUnderFear).
/// </summary>
[Flags]
public enum AbnormalCcFlags : long
{
    None       = 0,
    Blind      = 1,
    Paralyze   = 4,
    Sleep      = 8,
    Root       = 16,
    Silence    = 256,
    Fear       = 512,
    Stun       = 4096,
    Stumble    = 16384,
    Stagger    = 32768,
    OpenAerial = 65536,
    Spin       = 524288,
    CannotMove = 4194304,

    CantAttack = Spin | Sleep | Stun | Stumble | Stagger | OpenAerial | Paralyze | Fear | CannotMove,
    CantMove   = Spin | Root  | Sleep | Stumble | Stun | Stagger | OpenAerial | Paralyze | Fear | CannotMove,
}
