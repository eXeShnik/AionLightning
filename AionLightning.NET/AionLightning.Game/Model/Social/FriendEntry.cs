namespace AionLightning.Game.Model.Social;

public sealed record FriendEntry(
    int PlayerId,
    string Name,
    byte Level,
    PlayerClass PlayerClass,
    Race Race,
    string Note,            // private label stored in friend_list.note (not sent in packet)
    string PlayerNote);     // friend's own bio note from players.note (sent in SM_FRIEND_LIST)
