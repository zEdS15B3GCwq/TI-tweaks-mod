using UnityEngine;
using UnityModManagerNet;

namespace TITweaksMod
{
    internal sealed class SettingsUIContext
    {
        /// <summary>
        /// Struct to hold common styles, layout options, and helper functions for the settings UI.
        /// </summary>
        internal SettingsUIContext()
        {
            toggleStyle = new GUIStyle(GUI.skin.toggle) { contentOffset = new Vector2(8f, 0) };
            groupStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 10, 10) };
            sliderLayout = GUILayout.Width(200f);
            sliderLabelLayout = GUILayout.Width(50f);
        }

        internal GUIStyle toggleStyle { get; }
        internal GUIStyle groupStyle { get; }
        internal GUILayoutOption sliderLayout { get; }
        internal GUILayoutOption sliderLabelLayout { get; }
    }

    internal static class SettingsUI
    {
        internal static SettingsUIContext? context { get; private set; }

        /// <summary>
        /// Handles drawing the mod settings UI via UMM, invoked at each redraw.
        /// This method only draws the UI. Patch features define their own settings UI
        /// sub-sections, which are included here.
        /// </summary>
        /// <param name="modEntry">UMM mod context</param>
        internal static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (Main.Settings == null)
                return;

            context ??= new SettingsUIContext();

            /// Draw basic layout and title for the mod settings
            GUILayout.BeginVertical();

            GUILayout.Label($"{modEntry.Info.DisplayName} Mod Settings", UnityModManager.UI.h1);
            GUILayout.Space(10);

            MiningPatches.UI.OnGUI(Main.Settings.mineSettings, context);

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Called by UMM when the mod settings UI is closed/hidden.
        /// Features define their own handlers, which are included here.
        /// </summary>
        /// <param name="modEntry">UMM mod context</param>
        internal static void OnHideGUI(UnityModManager.ModEntry modEntry)
        {
            if (Main.Settings == null)
                return;
            MiningPatches.UI.OnHideGUI(Main.Settings.mineSettings);
        }
    }
}
