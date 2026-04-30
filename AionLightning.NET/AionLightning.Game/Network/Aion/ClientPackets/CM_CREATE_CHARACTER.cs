using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_CREATE_CHARACTER : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IPlayerDao _playerDao;
    private readonly IPlayerAppearanceDao _appearanceDao;
    private readonly IDataManager _dataManager;

    private string _name = string.Empty;
    private Gender _gender;
    private Race _race;
    private PlayerClass _class;
    private PlayerAppearance _appearance = new();
    private bool _checkOnly;

    public CM_CREATE_CHARACTER(GsClientConnection conn, IPlayerDao playerDao,
        IPlayerAppearanceDao appearanceDao, IDataManager dataManager)
    {
        _conn          = conn;
        _playerDao     = playerDao;
        _appearanceDao = appearanceDao;
        _dataManager   = dataManager;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadD();       // ignored
        r.ReadS();       // account info string — ignored

        _name = r.ReadS();
        r.ReadB(50 - (_name.Length * 2)); // skip fixed-width name field padding

        _gender = r.ReadD() == 0 ? Gender.MALE : Gender.FEMALE;
        _race   = r.ReadD() == 0 ? Race.ELYOS  : Race.ASMODIANS;
        _class  = PlayerClassExtensions.FromId((byte)r.ReadD());

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
        int armLength  = r.ReadC();
        int legLength  = r.ReadC();
        int shoulders  = r.ReadC();
        int faceShape  = r.ReadC();
        r.ReadC(); r.ReadC(); r.ReadC(); // ignored
        float height = r.ReadF();
        _checkOnly = r.ReadC() == 1;

        _appearance = new PlayerAppearance
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
        if (_checkOnly)
        {
            await _conn.SendAsync(new SM_CREATE_CHARACTER(SM_CREATE_CHARACTER.RESPONSE_CREATE_READY), ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(_name) || _name.Length < 2 || _name.Length > 16)
        {
            await _conn.SendAsync(new SM_CREATE_CHARACTER(SM_CREATE_CHARACTER.RESPONSE_INVALID_NAME), ct);
            return;
        }

        if (!_class.IsStartingClass())
        {
            await _conn.SendAsync(new SM_CREATE_CHARACTER(SM_CREATE_CHARACTER.FAILED_TO_CREATE), ct);
            return;
        }

        if (await _playerDao.ExistsByNameAsync(_name, ct))
        {
            await _conn.SendAsync(new SM_CREATE_CHARACTER(SM_CREATE_CHARACTER.RESPONSE_NAME_ALREADY_USED), ct);
            return;
        }

        var spawn = _dataManager.PlayerInitial.GetSpawnLocation(_race);
        var player = new Player
        {
            AccountId    = _conn.AccountId,
            Name         = _name,
            Level        = 1,
            Gender       = _gender,
            Race         = _race,
            PlayerClass  = _class,
            Position     = new Position(spawn.X, spawn.Y, spawn.Z, spawn.Heading, spawn.MapId),
            CreationDate = DateTime.UtcNow,
        };

        int newId;
        try
        {
            newId = await _playerDao.InsertAsync(player, ct);
        }
        catch
        {
            await _conn.SendAsync(new SM_CREATE_CHARACTER(SM_CREATE_CHARACTER.RESPONSE_DB_ERROR), ct);
            return;
        }

        player.ObjectId   = newId;
        player.Appearance = _appearance;

        try
        {
            await _appearanceDao.InsertAsync(newId, _appearance, ct);
        }
        catch
        {
            await _conn.SendAsync(new SM_CREATE_CHARACTER(SM_CREATE_CHARACTER.RESPONSE_DB_ERROR), ct);
            return;
        }

        await _conn.SendAsync(new SM_CREATE_CHARACTER(SM_CREATE_CHARACTER.RESPONSE_OK, player, _appearance), ct);
    }
}
