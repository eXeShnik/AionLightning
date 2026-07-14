namespace AionLightning.Game.Model.Instance;

/// <summary>
/// Dredgion scoreboard (Java <c>model.instance.instancereward.DredgionReward</c>): per-race point
/// totals, the 14 capturable rooms, and the winner/looser AP payout — Baranath Dredgion (300110000)
/// pays less than Chantra (300210000) / Terath (300440000), matching Java's mapId check.
/// </summary>
public sealed class DredgionReward : InstanceReward<DredgionPlayerReward>
{
    private const int BaranathMapId = 300110000;

    private int _asmodiansPoints;
    private int _elyosPoints;

    public int WinnerPoints { get; }
    public int LooserPoints { get; }
    public Race WinningRace { get; set; }

    public IReadOnlyList<DredgionRoom> Rooms { get; }

    /// <summary>Race-specific start position inside the ship, randomly assigned per run (Java setStartPositions).</summary>
    public (float X, float Y, float Z) AsmodiansStartPosition { get; }
    public (float X, float Y, float Z) ElyosStartPosition { get; }

    private static readonly (float X, float Y, float Z) PositionA = (570.468f, 166.897f, 432.28986f);
    private static readonly (float X, float Y, float Z) PositionB = (400.741f, 166.713f, 432.290f);

    public DredgionReward(int mapId, int instanceId) : base(mapId, instanceId)
    {
        WinnerPoints = mapId == BaranathMapId ? 3000 : 4500;
        LooserPoints = mapId == BaranathMapId ? 1500 : 2500;

        if (System.Random.Shared.Next(2) == 0)
        {
            AsmodiansStartPosition = PositionA;
            ElyosStartPosition     = PositionB;
        }
        else
        {
            AsmodiansStartPosition = PositionB;
            ElyosStartPosition     = PositionA;
        }

        var rooms = new List<DredgionRoom>(14);
        for (int i = 1; i <= 14; i++) rooms.Add(new DredgionRoom(i));
        Rooms = rooms;
    }

    public DredgionRoom? GetRoomById(int roomId) => Rooms.FirstOrDefault(r => r.RoomId == roomId);

    public void CaptureRoom(Race race, int roomId) => GetRoomById(roomId)?.Capture(race);

    public int GetPointsByRace(Race race) => race switch
    {
        Race.ELYOS     => _elyosPoints,
        Race.ASMODIANS => _asmodiansPoints,
        _ => 0,
    };

    public void AddPointsByRace(Race race, int points)
    {
        switch (race)
        {
            case Race.ELYOS:
                _elyosPoints = Math.Max(0, _elyosPoints + points);
                break;
            case Race.ASMODIANS:
                _asmodiansPoints = Math.Max(0, _asmodiansPoints + points);
                break;
        }
    }

    public Race WinningRaceByScore => _asmodiansPoints > _elyosPoints ? Race.ASMODIANS : Race.ELYOS;

    /// <summary>Room capture state (Java <c>DredgionReward.DredgionRooms</c>): 1 Primary Armory, 2 Backup
    /// Armory, 3 Gravity Control, 4 Engine Room, 5 Auxiliary Power, 6 Weapons Deck, 7 Lower Weapons Deck,
    /// 8/9 Ready Room 1/2, 10 Barracks, 11 Logistics Management, 12 Logistics Storage, 13 The Bridge,
    /// 14 Captain's Room.</summary>
    public sealed class DredgionRoom
    {
        public int RoomId { get; }
        public byte State { get; private set; } = 0xFF;

        public DredgionRoom(int roomId) => RoomId = roomId;

        public void Capture(Race race) => State = race == Race.ASMODIANS ? (byte)0x01 : (byte)0x00;
    }
}
