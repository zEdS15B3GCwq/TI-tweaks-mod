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

        private static readonly MethodHashSpec[] MethodHashes =
        [
            new MethodHashSpec(
                "TIFactionState.GetMissionControlRequirementFromMineNetwork(int)",
                AccessTools.Method(
                    typeof(TIFactionState),
                    nameof(TIFactionState.GetMissionControlRequirementFromMineNetwork),
                    [typeof(int)]
                ),
                Hash_TIFactionState_GetMissionControlRequirementFromMineNetwork
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
