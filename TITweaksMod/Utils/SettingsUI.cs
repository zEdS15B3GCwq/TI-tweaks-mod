using UnityEngine;
using UnityModManagerNet;

namespace TITweaksMod
{
    internal sealed class SettingsUIContext
    {
        internal SettingsUIContext()
        {
            toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                contentOffset = new Vector2(8f, 0),
                padding = new RectOffset(5, 0, 0, 0),
            };
            groupStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 10, 10) };
            sliderLayout = GUILayout.Width(200f);
            sliderLabelLayout = GUILayout.Width(50f);
        }

        internal GUIStyle toggleStyle { get; }
        internal GUIStyle groupStyle { get; }
        internal GUILayoutOption sliderLayout { get; }
        internal GUILayoutOption sliderLabelLayout { get; }

        internal float floatHorizontalSlider(
            in float oldValue,
            in float min,
            in float max,
            GUILayoutOption? layout = null
        )
        {
            float sliderValue = GUILayout.HorizontalSlider(
                oldValue,
                min,
                max,
                layout ?? sliderLayout
            );
            float newValue = Mathf.Clamp((float)Math.Round(sliderValue, 1), min, max);
            GUILayout.Label(newValue.ToString("0.0"), sliderLabelLayout);
            return newValue;
        }

        internal int intHorizontalSlider(
            in int oldValue,
            in int min,
            in int max,
            GUILayoutOption? layout = null
        )
        {
            float sliderValue = GUILayout.HorizontalSlider(
                oldValue,
                min,
                max,
                layout ?? sliderLayout
            );
            int newValue = Mathf.Clamp(Mathf.RoundToInt(sliderValue), min, max);
            GUILayout.Label(newValue.ToString("0.0"), sliderLabelLayout);
            return newValue;
        }
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
            NationPatches.UI.OnGUI(Main.Settings.nationSettings, context);

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
