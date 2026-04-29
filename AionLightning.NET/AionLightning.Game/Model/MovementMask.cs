namespace AionLightning.Game.Model;

public static class MovementMask
{
    public const byte StartMove = 0x01;
    public const byte Mouse     = 0x02;
    public const byte Glide     = 0x04;
    public const byte Vehicle   = 0x08;
    public const byte Fall      = 0x10;
}
