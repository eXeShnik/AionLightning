namespace AionLightning.Game.Model;

public readonly record struct Position(float X, float Y, float Z, int Heading, int WorldId, int InstanceId = 0);
