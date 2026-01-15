using PavonisInteractive.TerraInvicta.Actions;
using UnityEngine;
using UnityModManagerNet;

namespace TITweaksMod
{
    internal sealed class SettingsUIContext
    {
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

        internal static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (Main.Settings == null)
                return;

            context ??= new SettingsUIContext();

            GUILayout.BeginVertical();

            GUILayout.Label($"{modEntry.Info.DisplayName} Mod Settings", UnityModManager.UI.h1);
            GUILayout.Space(10);

            MiningPatches.UI.OnGUI(Main.Settings.mineSettings, context);

            GUILayout.EndVertical();
        }

        internal static void OnHideGUI(UnityModManager.ModEntry modEntry)
        {
            if (Main.Settings == null)
                return;
            MiningPatches.UI.OnHideGUI(Main.Settings.mineSettings);
        }
    }
}
