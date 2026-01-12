using HarmonyLib;
using PavonisInteractive.TerraInvicta;

namespace TILinearCostForMinesBeyondCapMod.patches
{
    [HarmonyPatch(typeof(TIFactionState), nameof(TIFactionState.GetMissionControlRequirementFromMineNetwork))]
    internal static class MineCostPatch
    {
        static bool Prefix(TIFactionState __instance, ref int __result, int mineNetworkSize = -1)
        {
            if (!Main.enabled)
                return true; // run original

            if (mineNetworkSize < 0)
                mineNetworkSize = __instance.MineNetworkSize;

            mineNetworkSize -= __instance.SafeMineNextworkSize;

            int k = (Main.settings != null) ? Main.settings.linearCostMultiplier : Main.DefaultLinearCostMultiplier;
            __result = mineNetworkSize > 0 ? mineNetworkSize * k : 0;
            return false;
        }
    }
}
