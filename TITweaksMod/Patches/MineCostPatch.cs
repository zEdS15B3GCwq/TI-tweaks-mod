using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using UnityEngine;

namespace TITweaksMod.patches
{
    [HarmonyPatch(
        typeof(TIFactionState),
        nameof(TIFactionState.GetMissionControlRequirementFromMineNetwork)
    )]
    internal static class MineCostPatch
    {
        // Prefix needs to capture the original mineNetworkSize parameter, as it has a default value,
        // and that's not passed to the Postfix as such. Postfix actually sees the last value of
        // mineNetworkSize in the original method, which is not the same as the input value.
        // This way the original method is unaffected and runs normally.
        static bool Prefix(out int __state, int mineNetworkSize)
        {
            __state = mineNetworkSize;
            return true;
        }

        // Mine cost tweaks are applied in Postfix - this allows other mods to patch the same function.
        static void Postfix(TIFactionState __instance, ref int __result, int __state)
        {
            if (!Main.enabled || Main.settings == null)
                return; // keep original

            // if linear cost is enabled, override original calculation and result
            if (Main.settings.mineLinearCostEnabled)
            {
                int mineNetworkSize = __state;
                if (mineNetworkSize < 0)
                    mineNetworkSize = __instance.MineNetworkSize;

                mineNetworkSize -= __instance.SafeMineNextworkSize;
                __result =
                    mineNetworkSize > 0 ? mineNetworkSize * Main.settings.mineLinearCostPerMine : 0;
            }

            // apply global cost multiplier if set
            if (Main.settings.mineGlobalCostMultiplier != 1.0f)
            {
                __result = Mathf.RoundToInt(__result * Main.settings.mineGlobalCostMultiplier);
            }
        }
    }
}
