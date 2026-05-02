namespace AionLightning.Game.Model;

/// <summary>
/// Bitmask of active CC states. Values match Java AbnormalState enum IDs
/// (com.aionemu.gameserver.skillengine.effect.AbnormalState).
/// CANT_ATTACK_STATE = Spin | Sleep | Stun | Stumble | Stagger | Paralyze | Fear.
/// </summary>
[Flags]
public enum AbnormalCcFlags : long
{
    None    = 0,
    Paralyze= 4,
    Sleep   = 8,
    Root    = 16,
    Silence = 256,
    Fear    = 512,
    Stun    = 4096,
    Stumble = 16384,
    Stagger = 32768,
    Spin    = 524288,

    CantAttack = Spin | Sleep | Stun | Stumble | Stagger | Paralyze | Fear,
    CantMove   = Spin | Root  | Sleep | Stumble | Stun | Stagger | Paralyze,
}
