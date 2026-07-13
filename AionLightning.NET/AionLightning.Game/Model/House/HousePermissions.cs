namespace AionLightning.Game.Model.House;

/// <summary>
/// Java model.house.HousePermissions — the door/notice state is bit-packed into <see cref="Model.House.House.Permissions"/>:
/// the low byte carries the notice state (SHOW_OWNER), the high byte carries the door state
/// (DOOR_OPENED_ALL / DOOR_OPENED_FRIENDS / DOOR_CLOSED). Kept as a static helper — packing/unpacking is
/// pure bit arithmetic, not stateful behavior.
/// </summary>
public static class HousePermissions
{
    public const int NOT_SET = 0;
    public const int SHOW_OWNER = 1;
    public const int DOOR_OPENED_ALL = 1 << 8;
    public const int DOOR_OPENED_FRIENDS = 2 << 8;
    public const int DOOR_CLOSED = 3 << 8;

    private static readonly int[] DoorStates = { DOOR_OPENED_ALL, DOOR_OPENED_FRIENDS, DOOR_CLOSED };

    public static int GetDoorState(int permissions)
    {
        int value = permissions & 0xFF00;
        return Array.IndexOf(DoorStates, value) >= 0 ? value : NOT_SET;
    }

    public static int SetDoorState(int permissions, int doorState)
    {
        int state = doorState & 0xFF00;
        return (permissions & 0x00FF) | state;
    }

    public static int GetNoticeState(int permissions) =>
        (permissions & SHOW_OWNER) == SHOW_OWNER ? SHOW_OWNER : NOT_SET;

    public static int SetNoticeState(int permissions, int noticeState) =>
        noticeState == NOT_SET ? permissions & 0xFF00 : (noticeState & 0xFF) | permissions;

    /// <summary>Java HousePermissions.getPacketValue() — the door state shifted down into a single byte.</summary>
    public static byte GetPacketValue(int doorState) => (byte)(doorState > 1 ? doorState >> 8 : doorState);
}
