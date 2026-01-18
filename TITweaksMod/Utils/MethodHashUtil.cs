using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using UnityModManagerNet;

namespace TITweaksMod
{
    internal static class MethodHashUtil
    {
        // Expected hashes (populated from game version 1.0.26)
        private const string Hash_TIFactionState_GetMissionControlRequirementFromMineNetwork =
            "A9B38584F8457697061D08116BFB03B113492B6A1C39AB1B950A21C13F425943";
        private const string Hash_TIFactionState_GetCurrentMiningMultiplierFromOrgsAndEffects =
            "95D8F4EA0FDE7033377E4DF96A7004F5BAF1EF6EE659B201A4DED1133B25D48A";
        private const string Hash_TIFactionState_GetYearlyIncome =
            "4FBC623C5B56F90B747EEECBC771B1573120084EE9720848EBE29A13F1F78A48";
        private const string Hash_TINationState_unrestRestState =
            "E9B0C762813A92D481529D21E8912F0C4FC519AE8B02AABB9D17AF56DFED1997";
        private const string Hash_TINationState_unrestRestState_unclamped =
            "642067BE7192756B6527648EFDC7891B91E176D58227AF60EF0120BF21003B4E";
        private const string Hash_TINationState_cohesionRestState =
            "ABFC5724C6A984234D1615CF6867DEF25027B02E88D2DA523CD0914CB88D094E";
        private const string Hash_TINationState_ClaimWillBeHostile =
            "03007EFA41B834EFFFD97911A233E9F000F46C658843F5055376465A7158DBAB";
        private const string Hash_TINationState_CanImproveRelationsYet =
            "CA69ECA1589FB4F5AC16F68E3E54C8F384331A83C55AD547A0B7D2C55D0A7230";
        private const string Hash_TINationState_MyClaimOnOtherCapital =
            "AD34D3783151FE787A4453A7855BC5B9A5F53120D3E0589B862956602B005DE6";

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
            new MethodHashSpec(
                "TINationState.MyClaimOnOtherCapital",
                AccessTools.Method(
                    typeof(TINationState),
                    nameof(TINationState.MyClaimOnOtherCapital),
                    [typeof(TINationState), typeof(bool), typeof(bool)]
                ),
                Hash_TINationState_MyClaimOnOtherCapital
            ),
        ];

        internal static string VerifyAll(UnityModManager.ModEntry.ModLogger logger, string modID)
        {
            string result = string.Empty;
            foreach (MethodHashSpec spec in MethodHashes)
            {
                string actual = Sha256Hex(spec.Method);
                if (string.IsNullOrEmpty(actual))
                {
                    logger?.Warning($"[{modID}] Hash check: could not hash method: {spec.Name}");
                    continue;
                }

                if (
                    !string.Equals(
                        actual,
                        spec.ExpectedSha256Hex,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    logger?.Warning(
                        $"[{modID}] Hash mismatch: game code changed; patch may need updating. "
                            + $"Method={spec.Name}, Expected={spec.ExpectedSha256Hex}, Actual={actual}"
                    );
                    if (string.IsNullOrEmpty(result))
                        result = actual;
                    else
                        result = string.Concat(result, ";", actual);
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
                Name = name;
                Method = method;
                ExpectedSha256Hex = expectedSha256Hex;
            }

            internal string Name { get; }
            internal MethodBase Method { get; }
            internal string ExpectedSha256Hex { get; }
        }
    }
}
