using HarmonyLib;
using PavonisInteractive.TerraInvicta;

namespace TITweaksMod.patches
{
    [HarmonyPatch(
        typeof(TIFactionState),
        nameof(TIFactionState.GetMissionControlRequirementFromMineNetwork)
    )]
    internal static class MineCostPatch
    {
        // Replaces GetMissionControlRequirementFromMineNetwork() to use a linear cost based on mine network size
        // instead of the original quadratic formula.
        // Cost is calculated as: max(0, (mineNetworkSize - safeMineNetworkSize) * k).
        // If the game changes how the safe size is applied, this patch will need to be updated.
        static bool Prefix(TIFactionState __instance, ref int __result, int mineNetworkSize = -1)
        {
            if (!Main.enabled || Main.settings == null)
                return true; // run original

            if (mineNetworkSize < 0)
                mineNetworkSize = __instance.MineNetworkSize;

            mineNetworkSize -= __instance.SafeMineNextworkSize;

            int k = Main.settings.linearCostMultiplier;
            __result = mineNetworkSize > 0 ? mineNetworkSize * k : 0;
            return false;
        }
    }
}
