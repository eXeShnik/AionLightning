using AionLightning.Commons.Network;
using AionLightning.Game.Model.Pet;
using AionLightning.Game.Model.Templates.Pet;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Multiplexed toy-pet packet (Java <c>SM_PET</c>, opcode 0x65). Keyed by an actionId that selects
/// the payload. P1 implements the ownership/lifecycle subtypes: 0 (list on login), 1 (adopt),
/// 2 (surrender), 3 (spawn), 4 (dismiss), 10 (rename). Feed(9)/mood(12)/doping(13) are P2.
/// </summary>
public sealed class SM_PET : AionServerPacket
{
    // PetFunctionType.getId() encodings (Java model/templates/pet/PetFunctionType.java: dataBitCount<<5 | id).
    private const int FnWarehouse  = 0;
    private const int FnFood       = 2049;   // 64<<5 | 1
    private const int FnDoping     = 8194;   // 256<<5 | 2
    private const int FnLoot       = 259;    // 8<<5 | 3
    private const int FnAppearance = 1;
    private const int FnNone       = 4;

    private readonly int _actionId;
    private IReadOnlyList<(PetCommonData Data, PetTemplate Template)>? _list;
    private PetCommonData? _common;
    private PetTemplate? _template;
    private Model.Pet.Pet? _pet;
    private int _objectId;
    private string? _name;

    private SM_PET(int actionId) : base(0x65) => _actionId = actionId;

    public static SM_PET List(IReadOnlyList<(PetCommonData, PetTemplate)> pets)
        => new(0) { _list = pets };

    public static SM_PET Adopt(PetCommonData data, PetTemplate template)
        => new(1) { _common = data, _template = template };

    public static SM_PET Surrender(PetCommonData data)
        => new(2) { _common = data };

    public static SM_PET Spawn(Model.Pet.Pet pet)
        => new(3) { _pet = pet };

    public static SM_PET Dismiss(int petObjectId)
        => new(4) { _objectId = petObjectId };

    public static SM_PET Rename(int petObjectId, string name)
        => new(10) { _objectId = petObjectId, _name = name };

    public override void Write(ref PacketWriter w)
    {
        w.WriteH(_actionId);
        switch (_actionId)
        {
            case 0:
                w.WriteC(0);
                w.WriteH((short)_list!.Count);
                foreach (var (data, tmpl) in _list!)
                    WritePetEntry(ref w, data, tmpl, adopt: false);
                break;
            case 1:
                WritePetEntry(ref w, _common!, _template!, adopt: true);
                break;
            case 2:
                w.WriteD(_common!.PetId);
                w.WriteD(_common.PetObjectId);
                w.WriteD(0);
                w.WriteD(0);
                break;
            case 3:
                WriteSpawn(ref w, _pet!);
                break;
            case 4:
                w.WriteD(_objectId);
                w.WriteC(0x01);
                break;
            case 10:
                w.WriteD(_objectId);
                w.WriteS(_name);
                break;
        }
    }

    private static void WritePetEntry(ref PacketWriter w, PetCommonData data, PetTemplate tmpl, bool adopt)
    {
        w.WriteS(data.Name);
        w.WriteD(data.PetId);
        w.WriteD(data.PetObjectId);
        w.WriteD(data.MasterObjectId);
        w.WriteD(0);
        w.WriteD(0);
        w.WriteD(ToUnixSeconds(data.Birthday));
        w.WriteD(AccompanyingSeconds(data.ExpireTime));

        int specialty = 0;
        if (HasFunction(tmpl, "WAREHOUSE")) { w.WriteH(FnWarehouse); specialty++; }
        if (HasFunction(tmpl, "LOOT"))      { w.WriteH(FnLoot); w.WriteC(0); specialty++; }
        if (HasFunction(tmpl, "DOPING"))
        {
            w.WriteH(FnDoping);
            if (adopt) { w.WriteQ(0); w.WriteQ(0); w.WriteQ(0); w.WriteQ(0); }
            else       { w.WriteD(0); w.WriteD(0); w.WriteQ(0); w.WriteQ(0); w.WriteQ(0); }
            specialty++;
        }
        if (HasFunction(tmpl, "FOOD"))
        {
            w.WriteH(FnFood);
            if (adopt) { w.WriteQ(0); }
            else       { w.WriteD(data.FeedProgress); w.WriteD(0); }
            specialty++;
        }

        // Pets carry at most 2 writable functions; pad the rest with NONE.
        if (specialty == 0) { w.WriteH(FnNone); w.WriteH(FnNone); }
        else if (specialty == 1) { w.WriteH(FnNone); }

        w.WriteH(FnAppearance);
        w.WriteC(0); w.WriteC(0); w.WriteC(0); // colour RGB (not implemented)
        w.WriteD(data.Decoration);

        w.WriteD(0); // epilog unk
        w.WriteD(0);
    }

    private static void WriteSpawn(ref PacketWriter w, Model.Pet.Pet pet)
    {
        w.WriteS(pet.CommonData.Name);
        w.WriteD(pet.CommonData.PetId);
        w.WriteD(pet.ObjectId);

        var master = pet.Master;
        var pos = pet.Position;
        if (pos.X == 0 && pos.Y == 0 && pos.Z == 0 && master is not null)
        {
            w.WriteF(master.Position.X); w.WriteF(master.Position.Y); w.WriteF(master.Position.Z);
            w.WriteF(master.Position.X); w.WriteF(master.Position.Y); w.WriteF(master.Position.Z);
            w.WriteC((byte)master.Position.Heading);
        }
        else
        {
            w.WriteF(pos.X); w.WriteF(pos.Y); w.WriteF(pos.Z);
            w.WriteF(pos.X); w.WriteF(pos.Y); w.WriteF(pos.Z);
            w.WriteC((byte)pos.Heading);
        }

        w.WriteD(master?.ObjectId ?? 0);
        w.WriteC(1);
        w.WriteD(0); // accompanying time
        w.WriteD(pet.CommonData.Decoration);
        w.WriteD(0); // wings id
        w.WriteD(0); // unk
    }

    private static bool HasFunction(PetTemplate tmpl, string type)
        => tmpl.Functions.Any(f => string.Equals(f.Type, type, StringComparison.OrdinalIgnoreCase));

    private static int ToUnixSeconds(DateTime dt)
        => dt == default ? 0 : (int)new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)).ToUnixTimeSeconds();

    private static int AccompanyingSeconds(DateTime? expire)
    {
        if (expire is null) return 0;
        long remain = new DateTimeOffset(DateTime.SpecifyKind(expire.Value, DateTimeKind.Utc)).ToUnixTimeSeconds()
                      - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return remain > 0 ? (int)remain : 0;
    }
}
