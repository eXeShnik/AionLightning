namespace AionLightning.Game.Model.Social;

public sealed record FriendEntry(
    int PlayerId,
    string Name,
    byte Level,
    PlayerClass PlayerClass,
    Race Race,
    string Note);
