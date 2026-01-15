using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace TITweaksMod
{
    internal static class Main
    {
        internal static bool enabled { get; private set; } = false;
        internal static Harmony? Harmony { get; private set; }
        internal static Settings? Settings { get; private set; }
        internal static UnityModManager.ModEntry.ModLogger? Logger { get; private set; }

        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            Harmony = new Harmony(modEntry.Info.Id);
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();

            string badHash = MethodHashUtil.VerifyAll(Logger, modEntry.Info.Id);
            if (!string.IsNullOrEmpty(badHash))
                Settings.dummyString = badHash;

            try
            {
                Harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch
            {
                Logger.Error($"{modEntry.Info.Id}: Error during patching in Load().");
                return false;
            }

            modEntry.OnGUI = SettingsUI.OnGUI;
            modEntry.OnHideGUI = SettingsUI.OnHideGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnToggle = OnToggle;

            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            return true;
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            UnityModManager.ModSettings.Save(Settings, modEntry);
        }
    }

    public sealed class Settings : UnityModManager.ModSettings
    {
        public bool modPatchOnLoad = true;
        public string dummyString = "";
        public MiningPatches.Settings mineSettings = new MiningPatches.Settings();
        public NationPatches.Settings nationSettings = new NationPatches.Settings();
    }
}
