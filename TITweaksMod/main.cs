using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace TITweaksMod
{
    internal static class Main
    {
        internal static Harmony harmony;
        internal static bool enabled;
        internal static Settings settings { get; private set; }
        internal static UnityModManager.ModEntry.ModLogger logger;
        internal static string modName { get; private set; } = "TITweaksMod";

        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            logger = modEntry.Logger;
            modName = string.IsNullOrEmpty(modEntry.Info.DisplayName)
                ? modEntry.Info.Id
                : modEntry.Info.DisplayName;

            settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();

            MethodHashUtil.VerifyAll(logger, modName);

            harmony = new Harmony(modEntry.Info.Id);

            if (settings.modPatchOnLoad)
                harmony.PatchAll(Assembly.GetExecutingAssembly());

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;

            if (enabled)
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            else
                harmony.UnpatchAll(harmony.Id);

            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mine cost multiplier:");

            // Slider always snaps to integers - no smooth dragging
            float sliderValue = GUILayout.HorizontalSlider(
                settings.linearCostMultiplier,
                1,
                15,
                GUILayout.Width(200)
            );
            settings.linearCostMultiplier = Mathf.Clamp(Mathf.RoundToInt(sliderValue), 1, 15);

            GUILayout.Label(settings.linearCostMultiplier.ToString());
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            UnityModManager.ModSettings.Save(settings, modEntry);
        }
    }

    public class Settings : UnityModManager.ModSettings
    {
        public bool modPatchOnLoad = true;
        public bool tweakMineCostEnabled = true;
        public int linearCostMultiplier = 6;
    }
}
