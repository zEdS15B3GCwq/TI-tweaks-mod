using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace TITweaksMod
{
    internal static class Main
    {
        private static bool loaded = false;
        private static bool patched = false;
        internal static bool enabled { get; private set; } = false;
        internal static Harmony? harmony { get; private set; }
        internal static Settings? settings { get; private set; }
        internal static UnityModManager.ModEntry.ModLogger? logger { get; private set; }
        internal static string modName { get; private set; } = "TITweaksMod";

        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            logger = modEntry.Logger;
            logger.Log($"{modName}: {modEntry.Info.DisplayName} {modEntry.Info.Id}");
            modName = string.IsNullOrEmpty(modEntry.Info.DisplayName)
                ? modEntry.Info.Id
                : modEntry.Info.DisplayName;
            string badHash = MethodHashUtil.VerifyAll(logger, modName);

            settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();
            if (!string.IsNullOrEmpty(badHash))
                settings.dummyString = badHash;
            harmony = new Harmony(modEntry.Info.Id);

            if (settings.modPatchOnLoad)
            {
                try
                {
                    harmony.PatchAll(Assembly.GetExecutingAssembly());
                }
                catch
                {
                    logger.Error($"{modName}: Error during patching in Load().");
                    return false;
                }
                patched = true;
            }

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = SettingsUI.OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            loaded = true;
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (!loaded)
                return false;

            if (settings == null || harmony == null)
            {
                logger?.Warning(
                    $"{modName}: OnToggle called before Load completed — aborting toggle."
                );
                return false; // signal failure to UMM
            }

            if (!settings.modPatchOnLoad)
            {
                try
                {
                    if (value && !patched)
                    {
                        harmony.PatchAll(Assembly.GetExecutingAssembly());
                        enabled = patched = true;
                    }
                }
                catch
                {
                    logger?.Error($"{modName}: Error during OnToggle patching.");
                    enabled = patched = false;
                    return false; // signal failure to UMM
                }

                try
                {
                    if (!value && patched)
                    {
                        harmony.UnpatchAll(harmony.Id);
                        enabled = patched = false;
                    }
                }
                catch
                {
                    logger?.Error($"{modName}: Error during OnToggle unpatching.");
                    enabled = false; // mod could be in unstable state if unpatch failed
                    patched = true;
                    return false; // signal failure to UMM
                }
            }
            else
                enabled = value;

            return true;
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            UnityModManager.ModSettings.Save(settings, modEntry);
        }
    }

    public class Settings : UnityModManager.ModSettings
    {
        public bool modPatchOnLoad = true;
        public bool mineLinearCostEnabled = false;
        public int mineLinearCostPerMine = 6;
        public float mineGlobalCostMultiplier = 1f;
        public string dummyString = "";
    }
}
