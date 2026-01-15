using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using UnityEngine;
using UnityModManagerNet;

namespace TITweaksMod.NationPatches
{
    internal static class UI
    {
        internal static void OnGUI(Settings settings, in SettingsUIContext context)
        {
            // group box
            GUILayout.BeginVertical(context.groupStyle);
            {
                // group label
                GUILayout.Label("Nation tweaks", UnityModManager.UI.h2);

                // TWEAK: shift base unrest
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("1. Change base unrest (default: 0.0, change to enable):");
                GUILayout.FlexibleSpace();
                settings.baseUnrestBonus = context.floatHorizontalSlider(
                    settings.baseUnrestBonus,
                    -10f,
                    10f
                );
                GUILayout.EndHorizontal();

                // TWEAK: shift base cohesion
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("1. Change base cohesion (default: 0.0, change to enable):");
                GUILayout.FlexibleSpace();
                settings.baseCohesionBonus = context.floatHorizontalSlider(
                    settings.baseCohesionBonus,
                    -10f,
                    10f
                );
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }
    }

    public class Settings : UnityModManager.ModSettings
    {
        public float baseUnrestBonus = 0f;
        public float baseCohesionBonus = 0f;
        public bool ignoreDemocracyDifferenceHostileClaims = false;
        public bool ignoreInnateHostileClaims = false;
    }
}
