namespace AionLightning.Game.Model;

[Flags]
public enum CreatureState
{
    None           = 0,
    Active         = 1,
    Dead           = 2,
    Resting        = 32,
    Walking        = 128,
    PrivateShop    = 256,
    WeaponEquipped = 512,
    Powershard     = 1024,
    Chair          = 2048,
    Flying         = 4096,
    Gliding        = 8192,
    Looting        = 16384,
}
