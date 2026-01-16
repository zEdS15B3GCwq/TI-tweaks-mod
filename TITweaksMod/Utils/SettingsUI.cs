using UnityEngine;
using UnityModManagerNet;

namespace TITweaksMod
{
    internal static class TextureStore
    {
        internal static Texture2D? ToggleOnTexture;
        internal static Texture2D? ToggleOffTexture;
        internal static Texture2D? ToolbarOnTexture;
        internal static Texture2D? ToolbarOffTexture;

        private static void BuildTextures()
        {
            ToggleOnTexture = CreateTexture(new Color(0.314f, 0.941f, 0.063f, 1.0f));
            ToggleOffTexture = CreateTexture(new Color(0.941f, 0.302f, 0.078f, 1.0f));
            ToolbarOnTexture = CreateTexture(new Color(0.941f, 0.71f, 0.098f, 1.0f));
            ToolbarOffTexture = CreateTexture(new Color(0.6f, 0.6f, 0.6f, 1.0f));
        }

        private static bool TexturesValid()
        {
            return (ToggleOnTexture is not null)
                && (ToggleOffTexture is not null)
                && (ToolbarOnTexture is not null)
                && (ToolbarOffTexture is not null);
        }

        internal static bool ValidateTextures()
        {
            if (!TexturesValid())
            {
                BuildTextures();
                return false;
            }
            return true;
        }

        private static Texture2D CreateTexture(Color color)
        {
            // 6x6: minimal but enough to hold a 2px border + 2x2 center fill.
            const int size = 6;
            const int border = 2;

            Color borderColor = new Color(0.20f, 0.20f, 0.20f, 1.0f);

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder =
                        x < border || x >= size - border || y < border || y >= size - border;
                    tex.SetPixel(x, y, isBorder ? borderColor : color);
                }
            }

            tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            UnityEngine.Object.DontDestroyOnLoad(tex);
            return tex;
        }
    }

    internal sealed class SettingsUIContext
    {
        internal SettingsUIContext()
        {
            GroupStyle = new(GUI.skin.box) { padding = new RectOffset(10, 10, 10, 10) };
            SliderLayout = GUILayout.Width(200f);
            WideSliderLayout = GUILayout.Width(500f);
            SliderLabelLayout = GUILayout.MinWidth(60f);

            // toggle button style: active - green, inactive - red
            ToggleStyle = new(GUI.skin.button);

            TextureStore.ValidateTextures();

            ToggleStyle.onNormal.background = TextureStore.ToggleOnTexture;
            ToggleStyle.onHover.background = TextureStore.ToggleOnTexture;
            ToggleStyle.onActive.background = TextureStore.ToggleOnTexture;
            ToggleStyle.onFocused.background = TextureStore.ToggleOnTexture;
            ToggleStyle.onNormal.textColor = Color.black;
            ToggleStyle.onHover.textColor = Color.black;
            ToggleStyle.onActive.textColor = Color.black;
            ToggleStyle.onFocused.textColor = Color.black;

            ToggleStyle.normal.background = TextureStore.ToggleOffTexture;
            ToggleStyle.hover.background = TextureStore.ToggleOffTexture;
            ToggleStyle.active.background = TextureStore.ToggleOffTexture;
            ToggleStyle.focused.background = TextureStore.ToggleOffTexture;
            ToggleStyle.normal.textColor = Color.black;
            ToggleStyle.hover.textColor = Color.black;
            ToggleStyle.active.textColor = Color.black;
            ToggleStyle.focused.textColor = Color.black;

            // ensure the 2px border is preserved when Unity stretches the background.
            ToggleStyle.border = new RectOffset(2, 2, 2, 2);

            // toolbar exclusive button style: active - orangeish, inactive - gray
            ToolbarStyle = new(GUI.skin.button);

            ToolbarStyle.onNormal.background = TextureStore.ToolbarOnTexture;
            ToolbarStyle.onHover.background = TextureStore.ToolbarOnTexture;
            ToolbarStyle.onActive.background = TextureStore.ToolbarOnTexture;
            ToolbarStyle.onFocused.background = TextureStore.ToolbarOnTexture;
            ToolbarStyle.onNormal.textColor = Color.black;
            ToolbarStyle.onHover.textColor = Color.black;
            ToolbarStyle.onActive.textColor = Color.black;
            ToolbarStyle.onFocused.textColor = Color.black;

            ToolbarStyle.normal.background = TextureStore.ToolbarOffTexture;
            ToolbarStyle.hover.background = TextureStore.ToolbarOffTexture;
            ToolbarStyle.active.background = TextureStore.ToolbarOffTexture;
            ToolbarStyle.focused.background = TextureStore.ToolbarOffTexture;
            ToolbarStyle.normal.textColor = Color.black;
            ToolbarStyle.hover.textColor = Color.black;
            ToolbarStyle.active.textColor = Color.black;
            ToolbarStyle.focused.textColor = Color.black;

            // Ensure the 2px border is preserved when Unity stretches the background.
            ToolbarStyle.border = new RectOffset(2, 2, 2, 2);
        }

        internal void ValidateStyles()
        {
            if (!TextureStore.ValidateTextures())
            {
                ToggleStyle.onNormal.background = TextureStore.ToggleOnTexture;
                ToggleStyle.onHover.background = TextureStore.ToggleOnTexture;
                ToggleStyle.onActive.background = TextureStore.ToggleOnTexture;
                ToggleStyle.onFocused.background = TextureStore.ToggleOnTexture;

                ToggleStyle.normal.background = TextureStore.ToggleOffTexture;
                ToggleStyle.hover.background = TextureStore.ToggleOffTexture;
                ToggleStyle.active.background = TextureStore.ToggleOffTexture;
                ToggleStyle.focused.background = TextureStore.ToggleOffTexture;

                ToggleStyle.border = new RectOffset(2, 2, 2, 2);

                ToolbarStyle.onNormal.background = TextureStore.ToolbarOnTexture;
                ToolbarStyle.onHover.background = TextureStore.ToolbarOnTexture;
                ToolbarStyle.onActive.background = TextureStore.ToolbarOnTexture;
                ToolbarStyle.onFocused.background = TextureStore.ToolbarOnTexture;

                ToolbarStyle.normal.background = TextureStore.ToolbarOffTexture;
                ToolbarStyle.hover.background = TextureStore.ToolbarOffTexture;
                ToolbarStyle.active.background = TextureStore.ToolbarOffTexture;
                ToolbarStyle.focused.background = TextureStore.ToolbarOffTexture;

                ToolbarStyle.border = new RectOffset(2, 2, 2, 2);
            }
        }

        internal GUIStyle ToggleStyle { get; private set; }
        internal GUIStyle GroupStyle { get; private set; }
        internal GUIStyle ToolbarStyle { get; private set; }
        internal GUILayoutOption SliderLayout { get; }
        internal GUILayoutOption WideSliderLayout { get; }
        internal GUILayoutOption SliderLabelLayout { get; }

        internal float FloatHorizontalSlider(
            in float oldValue,
            in float min,
            in float max,
            params GUILayoutOption[] layout
        )
        {
            if (layout.Length == 0)
                layout = [SliderLayout];
            float sliderValue = GUILayout.HorizontalSlider(oldValue, min, max, layout);
            float newValue = Mathf.Clamp((float)Math.Round(sliderValue, 1), min, max);
            GUILayout.Label(newValue.ToString("0.0"), SliderLabelLayout);
            return newValue;
        }

        internal int IntHorizontalSlider(
            in int oldValue,
            in int min,
            in int max,
            params GUILayoutOption[] layout
        )
        {
            if (layout.Length == 0)
                layout = [SliderLayout];
            float sliderValue = GUILayout.HorizontalSlider(oldValue, min, max, layout);
            int newValue = Mathf.Clamp(Mathf.RoundToInt(sliderValue), min, max);
            GUILayout.Label(newValue.ToString("0.0"), SliderLabelLayout);
            return newValue;
        }
    }

    internal static class SettingsUI
    {
        internal static SettingsUIContext? Context { get; private set; }

        /// <summary>
        /// Handles drawing the mod settings UI via UMM, invoked at each redraw.
        /// This method only draws the UI. Patch features define their own settings UI
        /// sub-sections, which are included here.
        /// </summary>
        /// <param name="modEntry">UMM mod context</param>
        internal static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (Main.Settings is null)
                return;

            Context ??= new SettingsUIContext();
            Context.ValidateStyles();

            /// Draw basic layout and title for the mod settings
            GUILayout.BeginVertical();

            GUILayout.Label($"{modEntry.Info.DisplayName} Mod Settings", UnityModManager.UI.h1);
            GUILayout.Space(10);

            MiningPatches.UI.OnGUI(Main.Settings.mineSettings, Context);
            NationPatches.UI.OnGUI(Main.Settings.nationSettings, Context);

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Called by UMM when the mod settings UI is closed/hidden.
        /// Features define their own handlers, which are included here.
        /// </summary>
        /// <param name="modEntry">UMM mod context</param>
        internal static void OnHideGUI(UnityModManager.ModEntry modEntry)
        {
            if (Main.Settings is null)
                return;
            MiningPatches.UI.OnHideGUI(Main.Settings.mineSettings);
        }
    }
}
