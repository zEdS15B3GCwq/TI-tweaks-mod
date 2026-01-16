using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using UnityModManagerNet;

namespace TITweaksMod
{
    internal static class MethodHashUtil
    {
        // Expected hashes (populate from a known-good game version)
        private const string Hash_TIFactionState_GetMissionControlRequirementFromMineNetwork =
            "A9B38584F8457697061D08116BFB03B113492B6A1C39AB1B950A21C13F425943";
        private const string Hash_TIFactionState_GetCurrentMiningMultiplierFromOrgsAndEffects = "";
        private const string Hash_TIFactionState_GetYearlyIncome = "";
        private const string Hash_TINationState_unrestRestState = "";
        private const string Hash_TINationState_unrestRestState_unclamped = "";
        private const string Hash_TINationState_cohesionRestState = "";
        private const string Hash_TINationState_ClaimWillBeHostile = "";
        private const string Hash_TINationState_CanImproveRelationsYet = "";

        private static readonly MethodHashSpec[] MethodHashes =
        [
            new MethodHashSpec(
                "TIFactionState.GetMissionControlRequirementFromMineNetwork",
                AccessTools.Method(
                    typeof(TIFactionState),
                    nameof(TIFactionState.GetMissionControlRequirementFromMineNetwork),
                    [typeof(int)]
                ),
                Hash_TIFactionState_GetMissionControlRequirementFromMineNetwork
            ),
            new MethodHashSpec(
                "TIFactionState.GetCurrentMiningMultiplierFromOrgsAndEffects",
                AccessTools.Method(
                    typeof(TIFactionState),
                    nameof(TIFactionState.GetCurrentMiningMultiplierFromOrgsAndEffects),
                    [typeof(FactionResource)]
                ),
                Hash_TIFactionState_GetCurrentMiningMultiplierFromOrgsAndEffects
            ),
            new MethodHashSpec(
                "TIFactionState.GetYearlyIncome",
                AccessTools.Method(
                    typeof(TIFactionState),
                    nameof(TIFactionState.GetYearlyIncome),
                    [typeof(FactionResource), typeof(bool), typeof(bool), typeof(bool)]
                ),
                Hash_TIFactionState_GetYearlyIncome
            ),
            new MethodHashSpec(
                "TINationState.unrestRestState.get",
                AccessTools.PropertyGetter(
                    typeof(TINationState),
                    nameof(TINationState.unrestRestState)
                ),
                Hash_TINationState_unrestRestState
            ),
            new MethodHashSpec(
                "TINationState.unrestRestState_unclamped.get",
                AccessTools.PropertyGetter(
                    typeof(TINationState),
                    nameof(TINationState.unrestRestState_unclamped)
                ),
                Hash_TINationState_unrestRestState_unclamped
            ),
            new MethodHashSpec(
                "TINationState.cohesionRestState.get",
                AccessTools.PropertyGetter(
                    typeof(TINationState),
                    nameof(TINationState.cohesionRestState)
                ),
                Hash_TINationState_cohesionRestState
            ),
            new MethodHashSpec(
                "TINationState.ClaimWillBeHostile",
                AccessTools.Method(
                    typeof(TINationState),
                    nameof(TINationState.ClaimWillBeHostile),
                    [typeof(TIRegionState)]
                ),
                Hash_TINationState_ClaimWillBeHostile
            ),
            new MethodHashSpec(
                "TINationState.CanImproveRelationsYet",
                AccessTools.Method(
                    typeof(TINationState),
                    nameof(TINationState.CanImproveRelationsYet),
                    [typeof(TINationState)]
                ),
                Hash_TINationState_CanImproveRelationsYet
            ),
        ];

        internal static string VerifyAll(UnityModManager.ModEntry.ModLogger logger, string modID)
        {
            string result = string.Empty;
            foreach (MethodHashSpec spec in MethodHashes)
            {
                string actual = Sha256Hex(spec.method);
                if (string.IsNullOrEmpty(actual))
                {
                    logger?.Warning($"[{modID}] Hash check: could not hash method: {spec.name}");
                    continue;
                }

                if (
                    !string.Equals(
                        actual,
                        spec.expectedSha256Hex,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    logger?.Warning(
                        $"[{modID}] Hash mismatch: game code changed; patch may need updating. "
                            + $"Method={spec.name}, Expected={spec.expectedSha256Hex}, Actual={actual}"
                    );
                    if (string.IsNullOrEmpty(result))
                        result = actual;
                    //else
                    //    string.Concat(result, ";", actual);
                }
            }
            return result;
        }

        private static string Sha256Hex(MethodBase method)
        {
            if (method is null)
                return string.Empty;

            byte[]? il = method.GetMethodBody()?.GetILAsByteArray();
            if (il is null || il.Length == 0)
                return string.Empty;

            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(il);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        private sealed class MethodHashSpec
        {
            internal MethodHashSpec(string name, MethodBase method, string expectedSha256Hex)
            {
                this.name = name;
                this.method = method;
                this.expectedSha256Hex = expectedSha256Hex;
            }

            internal string name { get; }
            internal MethodBase method { get; }
            internal string expectedSha256Hex { get; }
        }
    }
}
