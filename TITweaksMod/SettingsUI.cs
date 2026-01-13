using System.Net.NetworkInformation;
using UnityEngine;
using UnityModManagerNet;

namespace TITweaksMod
{
    internal static class SettingsUI
    {
        internal static Settings settings;
        internal static GUIStyle toggleStyle;
        internal static GUIStyle groupStyle;
        internal static GUIStyle sliderStyle;

        internal static void Init()
        {
            if (Main.settings == null)
                return;

            settings = Main.settings;

            toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.contentOffset = new Vector2(4, 0);

            groupStyle = new GUIStyle(GUI.skin.box);
            groupStyle.padding = new RectOffset(10, 10, 10, 10);

            sliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
            sliderStyle.fixedWidth = 200;
        }

        internal static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (settings == null)
                return;

            GUILayout.BeginVertical();

            GUILayout.Label($"{Main.modName} Mod Settings", UnityModManager.UI.h1);
            GUILayout.Space(10);

            MineSettings(settings);

            GUILayout.EndVertical();
        }

        private static void MineSettings(Settings settings)
        {
            // box group
            GUILayout.BeginVertical(groupStyle);

            // label
            GUILayout.Label("Linear mine cost beyond free mine limit", UnityModManager.UI.h2);

            // enable toggle
            settings.tweakMineCostEnabled = GUILayout.Toggle(
                settings.tweakMineCostEnabled,
                "Enable"
            );

            // cost multiplier slider
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mine cost multiplier:");

            // Slider always snaps to integers - no smooth dragging
            settings.linearCostMultiplier = IntegerSlider(settings.linearCostMultiplier, 1, 15);

            GUILayout.Label(settings.linearCostMultiplier.ToString());
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static int IntegerSlider(
            int currentValue,
            int minValue,
            int maxValue,
            GUILayoutOption[] options = null
        )
        {
            float sliderValue = GUILayout.HorizontalSlider(
                currentValue,
                minValue,
                maxValue,
                options ? options : sliderStyle
            );
            return Mathf.Clamp(Mathf.RoundToInt(sliderValue), minValue, maxValue);
        }
    }
}
