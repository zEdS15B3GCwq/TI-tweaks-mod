using PavonisInteractive.TerraInvicta.Actions;
using UnityEngine;
using UnityModManagerNet;

namespace TITweaksMod
{
    internal static class SettingsUI
    {
        internal static GUIStyle? toggleStyle;
        internal static GUIStyle? groupStyle;
        internal static GUILayoutOption? sliderLayout;

        internal static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (Main.settings == null)
                return;

            Settings settings = Main.settings;

            if (toggleStyle == null || groupStyle == null || sliderLayout == null)
            {
                toggleStyle = new GUIStyle(GUI.skin.toggle);
                toggleStyle.contentOffset = new Vector2(8f, 0);

                groupStyle = new GUIStyle(GUI.skin.box);
                groupStyle.padding = new RectOffset(10, 10, 10, 10);

                sliderLayout = GUILayout.Width(200f);
            }

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

            // group label
            GUILayout.Label("Mine cost tweaks", UnityModManager.UI.h2);

            // TWEAK: linear cost per mine above free cap
            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            GUILayout.Label("1. Linear cost above free limit (default: off):");
            GUILayout.Space(15);
            settings.mineLinearCostEnabled = GUILayout.Toggle(
                settings.mineLinearCostEnabled,
                "Enable",
                toggleStyle
            );
            GUILayout.FlexibleSpace();
            GUILayout.Label("Cost per mine:");
            float sliderValue = GUILayout.HorizontalSlider(
                settings.mineLinearCostPerMine,
                1f,
                15f,
                sliderLayout
            );
            settings.mineLinearCostPerMine = Mathf.Clamp(Mathf.RoundToInt(sliderValue), 1, 15);
            GUILayout.Label(settings.mineLinearCostPerMine.ToString());
            GUILayout.EndHorizontal();

            // TWEAK: global mine cost multiplier
            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            GUILayout.Label("2. Global mine cost multiplier (default: 1.0, change to enable):");
            GUILayout.FlexibleSpace();
            sliderValue = GUILayout.HorizontalSlider(
                settings.mineGlobalCostMultiplier,
                0f,
                2f,
                sliderLayout
            );
            settings.mineGlobalCostMultiplier = Mathf.Clamp(
                (float)Math.Round(sliderValue, 1),
                0f,
                2f
            );
            GUILayout.Label(settings.mineGlobalCostMultiplier.ToString());
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}
