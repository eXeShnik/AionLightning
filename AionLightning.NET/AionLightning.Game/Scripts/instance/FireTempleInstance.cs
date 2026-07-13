// Port of Java data/scripts/system/handlers/instance/FireTempleInstance.java (Gigi).
// Fire Temple (320100000): on channel creation, roll each boss slot 75/25 boss-vs-elite (one slot
// 90/10) and place Silver Blade Rotan at one of three spots. Each channel gets its own roll, proving
// per-channel scripted spawns land in the correct instance scope.
using AionLightning.Game.Instance;
using AionLightning.Game.World;

namespace Instance;

[InstanceId(320100000)]
public sealed class FireTempleInstance : GeneralInstanceHandler
{
    private static int Roll(int min, int max) => System.Random.Shared.Next(min, max + 1);

    public override void OnInstanceCreate(WorldMapInstance instance)
    {
        // Blue Crystal Molgat (else elite)
        Spawn(Roll(1, 100) > 25 ? 212839 : 212790, 127.1218f, 176.1912f, 99.67548f, 15);
        // Black Smoke Asparn (else elite)
        Spawn(Roll(1, 100) > 25 ? 212842 : 212799, 322.3193f, 431.2696f, 134.5296f, 80);
        // Lava Gatneri (else elite)
        Spawn(Roll(1, 100) > 25 ? 212840 : 212794, 153.0038f, 299.7786f, 123.0186f, 30);
        // Tough Sipus (else elite — note: elite heading 15 in Java, boss heading 30)
        if (Roll(1, 100) > 25) Spawn(212843, 296.6911f, 201.9092f, 119.3652f, 30);
        else                   Spawn(212803, 296.6911f, 201.9092f, 119.3652f, 15);
        // Flame Branch Flavi (else elite)
        Spawn(Roll(1, 100) > 25 ? 212841 : 212799, 350.9276f, 351.7389f, 146.8498f, 45);
        // Broken Wing Kutisen (else elite)
        Spawn(Roll(1, 100) > 25 ? 212845 : 214094, 298.7095f, 89.42245f, 128.7143f, 15);
        // Kromede: 10% chance of the stronger variant
        Spawn(Roll(1, 100) > 90 ? 214621 : 212846, 421.9935f, 93.18915f, 117.3053f, 46);

        // Silver Blade Rotan at one of three spots
        switch (Roll(1, 3))
        {
            case 1: Spawn(212844, 216.35815f, 264.34009f, 120.931f,    90); break;
            case 2: Spawn(212844, 277.70825f, 248.30695f, 121.067665f, 90); break;
            default: Spawn(212844, 290.94812f, 178.18243f, 119.29246f, 90); break;
        }
    }
}
