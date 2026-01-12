using HarmonyLib;
using UnityModManagerNet;
using System.Reflection;
using UnityEngine;

namespace TILinearCostForMinesBeyondCapMod
{
    internal static class Main
    {
        internal static Harmony harmony;
        internal static bool enabled;
        internal static Settings settings;

        // Default multiplier constant
        internal const int DefaultLinearCostMultiplier = 5;

        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();

            harmony = new Harmony(modEntry.Info.Id);

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
            float sliderValue = GUILayout.HorizontalSlider(settings.linearCostMultiplier, 1, 15, GUILayout.Width(200));
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
        // default multiplier
        public int linearCostMultiplier = Main.DefaultLinearCostMultiplier;
    }
}
