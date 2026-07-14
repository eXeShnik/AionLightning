// ScaldingExecutorAI2 — Java ai/instance/raksang/ScaldingExecutorAI2.java. Puzzle add: charges toward
// its target, destroys it, then floods the area with a target-specific swarm of hazard props.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("scalding_executor")]
public sealed class ScaldingExecutorAI2 : AggressiveNpcAI2
{
    private bool _isDestroyed;

    // note: Java also overrode canThink to always return false — no C# equivalent hook exists.

    public override void OnSpawned()
    {
        base.OnSpawned();
        if (State != AiState.Following)
            State = AiState.Following;
    }

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        if (_isDestroyed) return;
        _isDestroyed = true;
        if (getOwner().IsAlreadyDead) return;

        // note: Java aborted the move controller here; move-controller access isn't exposed to scripts
        // yet.
        if (getOwner().Target is not Npc npc) return;
        int targetId = npc.Template.NpcId;
        UseSkill(19928, 44);
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead) return;
            SpawnSwarm(targetId);
            // note: Java deleted itself via AI2Actions.deleteOwner; no scripted despawn API exists yet.
        }, 5300);
    }

    private void SpawnSwarm(int targetId)
    {
        switch (targetId)
        {
            case 701062:
                Spawn(282455, 783.102f, 958.593f, 792.070f, 96);
                Spawn(282455, 791.766f, 973.870f, 792.070f, 96);
                Spawn(282455, 788.129f, 972.133f, 792.070f, 96);
                Spawn(282455, 785.073f, 963.853f, 792.070f, 96);
                Spawn(282455, 777.910f, 969.496f, 792.012f, 96);
                Spawn(282455, 788.713f, 965.515f, 792.070f, 96);
                Spawn(282455, 780.973f, 977.756f, 792.010f, 96);
                Spawn(282455, 793.498f, 970.167f, 792.070f, 96);
                Spawn(282455, 786.981f, 969.063f, 792.070f, 96);
                Spawn(282455, 797.225f, 971.849f, 791.947f, 96);
                Spawn(282455, 783.309f, 967.476f, 791.979f, 96);
                Spawn(282455, 793.705f, 979.130f, 792.070f, 96);
                Spawn(282455, 784.660f, 979.440f, 791.967f, 96);
                Spawn(282455, 781.370f, 962.248f, 792.070f, 96);
                Spawn(282455, 788.298f, 981.132f, 792.070f, 96);
                Spawn(282455, 786.551f, 984.795f, 791.996f, 96);
                Spawn(282455, 794.169f, 963.423f, 791.947f, 96);
                Spawn(282455, 792.399f, 967.109f, 792.070f, 96);
                Spawn(282455, 798.938f, 968.151f, 792.070f, 96);
                Spawn(282455, 786.391f, 975.792f, 791.971f, 96);
                Spawn(282455, 779.674f, 965.888f, 791.971f, 96);
                Spawn(282455, 777.703f, 960.644f, 791.985f, 96);
                Spawn(282455, 791.955f, 982.828f, 792.070f, 96);
                Spawn(282455, 790.210f, 986.454f, 791.986f, 96);
                Spawn(282455, 790.036f, 977.504f, 792.070f, 96);
                Spawn(282455, 797.842f, 965.118f, 792.070f, 96);
                Spawn(282455, 786.790f, 960.208f, 792.070f, 96);
                Spawn(282455, 790.450f, 961.818f, 792.014f, 96);
                Spawn(282455, 782.721f, 974.117f, 792.011f, 96);
                Spawn(282455, 781.577f, 971.099f, 792.008f, 96);
                Spawn(282455, 795.453f, 975.459f, 791.983f, 96);
                break;
            case 701063:
                Spawn(282456, 799.578f, 986.389f, 792.070f, 96);
                Spawn(282456, 795.826f, 984.700f, 792.070f, 96);
                Spawn(282456, 797.627f, 981.004f, 792.070f, 96);
                Spawn(282456, 799.378f, 977.320f, 791.947f, 96);
                Spawn(282456, 797.796f, 990.047f, 791.996f, 96);
                Spawn(282456, 794.100f, 988.453f, 791.995f, 96);
                Spawn(282456, 815.608f, 986.367f, 791.997f, 96);
                Spawn(282456, 824.537f, 977.075f, 791.974f, 96);
                Spawn(282456, 813.650f, 981.030f, 792.051f, 96);
                Spawn(282456, 808.043f, 974.108f, 792.070f, 96);
                Spawn(282456, 808.797f, 985.932f, 792.026f, 96);
                Spawn(282456, 811.719f, 975.756f, 792.070f, 96);
                Spawn(282456, 803.077f, 979.008f, 792.070f, 96);
                Spawn(282456, 805.084f, 984.303f, 791.978f, 96);
                Spawn(282456, 801.348f, 982.667f, 792.070f, 96);
                Spawn(282456, 815.380f, 977.402f, 792.070f, 96);
                Spawn(282456, 810.006f, 979.367f, 792.070f, 96);
                Spawn(282456, 820.838f, 975.433f, 792.070f, 96);
                Spawn(282456, 822.807f, 980.770f, 791.985f, 96);
                Spawn(282456, 819.058f, 979.060f, 792.070f, 96);
                Spawn(282456, 811.896f, 984.688f, 792.034f, 96);
                Spawn(282456, 806.106f, 968.819f, 792.070f, 96);
                Spawn(282456, 817.120f, 973.759f, 792.070f, 96);
                Spawn(282456, 802.930f, 970.023f, 792.070f, 96);
                Spawn(282456, 806.978f, 989.706f, 792.007f, 96);
                Spawn(282456, 803.365f, 988.028f, 791.950f, 96);
                Spawn(282456, 817.264f, 982.687f, 791.953f, 96);
                Spawn(282456, 801.182f, 973.680f, 791.947f, 96);
                Spawn(282456, 806.846f, 980.603f, 792.070f, 96);
                Spawn(282456, 804.878f, 975.303f, 792.070f, 96);
                Spawn(282456, 809.804f, 970.483f, 791.947f, 96);
                Spawn(282456, 813.482f, 972.145f, 791.947f, 96);
                break;
            case 701064:
                Spawn(282457, 824.247f, 958.754f, 792.014f, 96);
                Spawn(282457, 810.332f, 954.320f, 791.969f, 96);
                Spawn(282457, 817.708f, 957.578f, 792.070f, 96);
                Spawn(282457, 812.298f, 959.597f, 792.070f, 96);
                Spawn(282457, 814.023f, 955.937f, 792.070f, 96);
                Spawn(282457, 813.689f, 947.049f, 792.070f, 96);
                Spawn(282457, 817.398f, 948.684f, 792.070f, 96);
                Spawn(282457, 815.376f, 943.375f, 792.001f, 96);
                Spawn(282457, 824.873f, 951.951f, 792.011f, 96);
                Spawn(282457, 821.147f, 950.299f, 791.993f, 96);
                Spawn(282457, 819.437f, 953.941f, 792.008f, 96);
                Spawn(282457, 815.745f, 952.323f, 792.070f, 96);
                Spawn(282457, 808.600f, 958.016f, 791.947f, 96);
                Spawn(282457, 811.984f, 950.646f, 792.070f, 96);
                Spawn(282457, 823.100f, 955.615f, 792.015f, 96);
                Spawn(282457, 819.090f, 945.042f, 792.002f, 96);
                Spawn(282457, 824.448f, 967.711f, 792.070f, 96);
                Spawn(282457, 813.423f, 962.790f, 792.070f, 96);
                Spawn(282457, 815.361f, 968.058f, 791.945f, 96);
                Spawn(282457, 822.699f, 971.376f, 792.070f, 96);
                Spawn(282457, 817.119f, 964.428f, 792.070f, 96);
                Spawn(282457, 806.847f, 961.678f, 792.070f, 96);
                Spawn(282457, 828.108f, 969.318f, 791.986f, 96);
                Spawn(282457, 822.512f, 962.405f, 792.009f, 96);
                Spawn(282457, 826.415f, 973.094f, 791.976f, 96);
                Spawn(282457, 820.776f, 966.065f, 792.070f, 96);
                Spawn(282457, 826.156f, 963.993f, 791.988f, 96);
                Spawn(282457, 818.852f, 960.782f, 792.070f, 96);
                Spawn(282457, 807.990f, 964.756f, 792.070f, 96);
                Spawn(282457, 811.709f, 966.403f, 791.947f, 96);
                Spawn(282457, 819.039f, 969.725f, 792.070f, 96);
                Spawn(282457, 827.893f, 960.347f, 792.011f, 96);
                break;
            case 701065:
                Spawn(282458, 785.103f, 954.307f, 792.070f, 96);
                Spawn(282458, 797.740f, 955.561f, 792.070f, 96);
                Spawn(282458, 798.785f, 949.137f, 792.070f, 96);
                Spawn(282458, 796.009f, 959.214f, 791.947f, 96);
                Spawn(282458, 795.791f, 950.249f, 792.070f, 96);
                Spawn(282458, 788.540f, 946.983f, 791.990f, 96);
                Spawn(282458, 794.107f, 953.926f, 792.070f, 96);
                Spawn(282458, 786.846f, 950.660f, 792.070f, 96);
                Spawn(282458, 793.880f, 944.954f, 792.035f, 96);
                Spawn(282458, 790.245f, 943.348f, 791.994f, 96);
                Spawn(282458, 781.493f, 952.659f, 791.966f, 96);
                Spawn(282458, 788.748f, 955.924f, 792.070f, 96);
                Spawn(282458, 783.225f, 949.079f, 791.978f, 96);
                Spawn(282458, 798.542f, 940.225f, 792.003f, 96);
                Spawn(282458, 792.147f, 948.576f, 792.062f, 96);
                Spawn(282458, 790.465f, 952.281f, 792.070f, 96);
                Spawn(282458, 792.394f, 957.606f, 791.947f, 96);
                Spawn(282458, 796.852f, 943.859f, 792.032f, 96);
                Spawn(282458, 809.621f, 945.154f, 792.070f, 96);
                Spawn(282458, 811.347f, 941.537f, 792.000f, 96);
                Spawn(282458, 802.238f, 941.889f, 791.985f, 96);
                Spawn(282458, 807.898f, 948.790f, 792.070f, 96);
                Spawn(282458, 802.509f, 950.785f, 792.070f, 96);
                Spawn(282458, 804.470f, 956.056f, 791.947f, 96);
                Spawn(282458, 807.624f, 939.872f, 791.999f, 96);
                Spawn(282458, 804.221f, 947.156f, 792.070f, 96);
                Spawn(282458, 800.525f, 945.492f, 791.991f, 96);
                Spawn(282458, 800.770f, 954.425f, 792.070f, 96);
                Spawn(282458, 805.914f, 943.520f, 792.070f, 96);
                Spawn(282458, 802.725f, 959.777f, 792.070f, 96);
                Spawn(282458, 806.177f, 952.464f, 791.947f, 96);
                Spawn(282458, 799.656f, 960.882f, 792.070f, 96);
                break;
        }
    }

    // note: Java also overrode ask (CAN_RESIST_ABNORMAL = POSITIVE) and pollInstance
    // (SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD = NEGATIVE) — neither hook exists on NpcAi2.

    public override void OnDied()
    {
        base.OnDied();
        // note: Java deleted itself via AI2Actions.deleteOwner; no scripted despawn API exists yet.
    }
}
