using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using System;
using System.Reflection;
using System.Security.Cryptography;
using UnityModManagerNet;

namespace TITweaksMod
{
    internal static class MethodHashUtil
    {
        // Expected hashes (populate from a known-good game version)
        private const string Hash_TIFactionState_GetMissionControlRequirementFromMineNetwork_Int32 = "PUT_EXPECTED_HASH_HERE";

        private static readonly MethodHashSpec[] MethodHashes = new[]
        {
            new MethodHashSpec(
                "TIFactionState.GetMissionControlRequirementFromMineNetwork(int)",
                AccessTools.Method(
                    typeof(TIFactionState),
                    nameof(TIFactionState.GetMissionControlRequirementFromMineNetwork),
                    new[] { typeof(int) }),
                Hash_TIFactionState_GetMissionControlRequirementFromMineNetwork_Int32),
        };

        internal static void VerifyAll(UnityModManager.ModEntry.ModLogger logger, string ModName)
        {
            for (int i = 0; i < MethodHashes.Length; i++)
            {
                MethodHashSpec spec = MethodHashes[i];

                if (spec.Method == null)
                {
                    logger?.Warning($"[{ModName}] Hash check: method not found: {spec.Name}");
                    continue;
                }

                string actual = Sha256Hex(spec.Method);
                if (actual == null)
                {
                    logger?.Warning($"[{ModName}] Hash check: could not hash method: {spec.Name}");
                    continue;
                }

                if (!string.Equals(actual, spec.ExpectedSha256Hex, StringComparison.OrdinalIgnoreCase))
                {
                    logger?.Warning(
                        $"[{ModName}] Hash mismatch: game code changed; patch may need updating. " +
                        $"Method={spec.Name}, Expected={spec.ExpectedSha256Hex}, Actual={actual}");
                }
            }
        }

        private static string Sha256Hex(MethodBase method)
        {
            if (method == null)
                return null;

            byte[] il = method.GetMethodBody()?.GetILAsByteArray();
            if (il == null || il.Length == 0)
                return null;

            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(il);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        private readonly struct MethodHashSpec
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
