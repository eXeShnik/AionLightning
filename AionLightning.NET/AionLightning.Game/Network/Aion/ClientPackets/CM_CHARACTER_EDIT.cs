using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client edits character appearance at the barbershop (plastic surgery / gender change). Opcode 0xA5.</summary>
public sealed class CM_CHARACTER_EDIT : AionClientPacket
{
    private readonly GsClientConnection      _conn;
    private readonly IPlayerDao              _playerDao;
    private readonly IItemDao                _itemDao;
    private readonly IPlayerAppearanceDao    _appearanceDao;
    private readonly PlayerEnterWorldService _enterWorldService;

    private int _objectId;
    private int _genderRaw;
    private PlayerAppearance _newAppearance = new();

    // Plastic surgery ticket item IDs (169650000–169650007)
    private static readonly int[] SurgeryTicketIds =
        [169650000, 169650001, 169650002, 169650003, 169650004, 169650005, 169650006, 169650007];

    // Gender change ticket item IDs (169660000–169660002)
    private static readonly int[] GenderTicketIds =
        [169660000, 169660001, 169660002];

    public CM_CHARACTER_EDIT(GsClientConnection conn, IPlayerDao playerDao, IItemDao itemDao,
        IPlayerAppearanceDao appearanceDao, PlayerEnterWorldService enterWorldService)
    {
        _conn              = conn;
        _playerDao         = playerDao;
        _itemDao           = itemDao;
        _appearanceDao     = appearanceDao;
        _enterWorldService = enterWorldService;
    }

    public override void Read(ref PacketReader r)
    {
        _objectId = r.ReadD();
        r.ReadB(52);      // original character data echoed by client — not used server-side
        _genderRaw = r.ReadD();
        r.ReadD();        // race (immutable — ignored)
        r.ReadD();        // player class (immutable — ignored)

        int voice   = r.ReadD();
        int skinRgb = r.ReadD();
        int hairRgb = r.ReadD();
        int eyeRgb  = r.ReadD();
        int lipRgb  = r.ReadD();

        int face        = r.ReadC();
        int hair        = r.ReadC();
        int deco        = r.ReadC();
        int tattoo      = r.ReadC();
        int faceContour = r.ReadC();
        int expression  = r.ReadC();
        r.ReadC(); // always 4
        int jawLine  = r.ReadC();
        int forehead = r.ReadC();

        int eyeHeight = r.ReadC();
        int eyeSpace  = r.ReadC();
        int eyeWidth  = r.ReadC();
        int eyeSize   = r.ReadC();
        int eyeShape  = r.ReadC();
        int eyeAngle  = r.ReadC();

        int browHeight = r.ReadC();
        int browAngle  = r.ReadC();
        int browShape  = r.ReadC();

        int nose       = r.ReadC();
        int noseBridge = r.ReadC();
        int noseWidth  = r.ReadC();
        int noseTip    = r.ReadC();

        int cheek     = r.ReadC();
        int lipHeight = r.ReadC();
        int mouthSize = r.ReadC();
        int lipSize   = r.ReadC();
        int smile     = r.ReadC();
        int lipShape  = r.ReadC();
        int jawHeight = r.ReadC();
        int chinJut   = r.ReadC();
        int earShape  = r.ReadC();
        int headSize  = r.ReadC();

        int neck       = r.ReadC();
        int neckLength = r.ReadC();
        int shoulderSize = r.ReadC();
        int torso        = r.ReadC();
        int chest        = r.ReadC();
        int waist        = r.ReadC();
        int hips         = r.ReadC();
        int armThickness = r.ReadC();
        int handSize     = r.ReadC();
        int legThickness = r.ReadC();
        int footSize     = r.ReadC();
        int facialRate   = r.ReadC();

        r.ReadC(); // always 0
        int armLength = r.ReadC();
        int legLength = r.ReadC();
        int shoulders = r.ReadC();
        int faceShape = r.ReadC();
        r.ReadC(); r.ReadC(); r.ReadC(); // ignored
        float height = r.ReadF();

        _newAppearance = new PlayerAppearance
        {
            Voice        = voice,
            SkinRgb      = skinRgb,
            HairRgb      = hairRgb,
            EyeRgb       = eyeRgb,
            LipRgb       = lipRgb,
            Face         = face,
            Hair         = hair,
            Deco         = deco,
            Tattoo       = tattoo,
            FaceContour  = faceContour,
            Expression   = expression,
            JawLine      = jawLine,
            Forehead     = forehead,
            EyeHeight    = eyeHeight,
            EyeSpace     = eyeSpace,
            EyeWidth     = eyeWidth,
            EyeSize      = eyeSize,
            EyeShape     = eyeShape,
            EyeAngle     = eyeAngle,
            BrowHeight   = browHeight,
            BrowAngle    = browAngle,
            BrowShape    = browShape,
            Nose         = nose,
            NoseBridge   = noseBridge,
            NoseWidth    = noseWidth,
            NoseTip      = noseTip,
            Cheek        = cheek,
            LipHeight    = lipHeight,
            MouthSize    = mouthSize,
            LipSize      = lipSize,
            Smile        = smile,
            LipShape     = lipShape,
            JawHeight    = jawHeight,
            ChinJut      = chinJut,
            EarShape     = earShape,
            HeadSize     = headSize,
            Neck         = neck,
            NeckLength   = neckLength,
            ShoulderSize = shoulderSize,
            Torso        = torso,
            Chest        = chest,
            Waist        = waist,
            Hips         = hips,
            ArmThickness = armThickness,
            HandSize     = handSize,
            LegThickness = legThickness,
            FootSize     = footSize,
            FacialRate   = facialRate,
            ArmLength    = armLength,
            LegLength    = legLength,
            Shoulders    = shoulders,
            FaceShape    = faceShape,
            Height       = height,
        };
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = await _playerDao.FindByObjectIdAsync(_objectId, ct);
        if (player is null || player.AccountId != _conn.AccountId)
        {
            await _conn.DisposeAsync();
            return;
        }

        bool genderChange = (int)player.Gender != _genderRaw;
        var ticketIds     = genderChange ? GenderTicketIds : SurgeryTicketIds;

        // Load inventory to find and consume ticket; always enter world regardless of outcome
        var allItems = (await _itemDao.FindByPlayerIdAsync(_objectId, ct)).ToList();
        var ticket   = allItems.FirstOrDefault(i => Array.IndexOf(ticketIds, i.ItemId) >= 0 && i.Count > 0);

        if (ticket is not null)
        {
            // Consume ticket, save updated appearance and (if needed) gender to DB before entering world
            if (ticket.Count == 1)
                allItems.Remove(ticket);
            else
                ticket.Count--;
            await _itemDao.SaveAllAsync(_objectId, allItems, ct);

            await _appearanceDao.UpdateAsync(_objectId, _newAppearance, ct);

            if (genderChange)
                await _playerDao.UpdateGenderAsync(_objectId,
                    _genderRaw == 0 ? Gender.MALE : Gender.FEMALE, ct);
        }

        // Always enter world; if no ticket, the player enters with their existing appearance
        await _enterWorldService.EnterWorldAsync(_conn, _objectId, ct);

        // Send error AFTER entering world (mirrors Java behavior: enterWorld is unconditional)
        if (ticket is null && _conn.ActivePlayer is not null)
        {
            var msg = genderChange
                ? SM_SYSTEM_MESSAGE.CharEditNoGenderTicket()
                : SM_SYSTEM_MESSAGE.CharEditNoPlasticSurgeryTicket();
            try { await _conn.SendAsync(msg, ct); } catch { }
        }
    }
}
