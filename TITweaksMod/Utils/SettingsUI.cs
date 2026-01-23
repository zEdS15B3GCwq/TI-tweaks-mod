using UnityEngine;
using UnityModManagerNet;

namespace TITweaksMod
{
    internal static class TextureStore
    {
        internal static Texture2D? GreenTexture;
        internal static Texture2D? RedTexture;
        internal static Texture2D? YellowTexture;
        internal static Texture2D? GrayTexture;
        internal static Texture2D? BlueTexture;

        private static void BuildTextures()
        {
            GreenTexture = CreateTexture(new Color(0.314f, 0.941f, 0.063f, 1.0f));
            RedTexture = CreateTexture(new Color(0.941f, 0.302f, 0.078f, 1.0f));
            YellowTexture = CreateTexture(new Color(0.941f, 0.71f, 0.098f, 1.0f));
            GrayTexture = CreateTexture(new Color(0.6f, 0.6f, 0.6f, 1.0f));
            BlueTexture = CreateTexture(new Color(0.09f, 0.424f, 0.922f, 1.0f));
        }

        private static bool TexturesValid()
        {
            return (GreenTexture is not null)
                && (RedTexture is not null)
                && (YellowTexture is not null)
                && (BlueTexture is not null)
                && (GrayTexture is not null);
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
            MinimalPadding = new(GUI.skin.button) { padding = new RectOffset(3, 3, 3, 3) };

            TextureStore.ValidateTextures();

            // toggle button style: active - green, inactive - red
            ToggleStyle = CreateStyle(
                GUI.skin.button,
                TextureStore.RedTexture,
                TextureStore.GreenTexture,
                Color.black,
                Color.black
            );

            // toolbar exclusive button style: active - orangeish, inactive - gray
            ToolbarStyle = CreateStyle(
                GUI.skin.button,
                TextureStore.GrayTexture,
                TextureStore.YellowTexture,
                Color.black,
                Color.black
            );

            // selection grid button: same as toolbar just less padding
            GridStyle = CreateStyle(
                GUI.skin.button,
                TextureStore.GrayTexture,
                TextureStore.YellowTexture,
                Color.black,
                Color.black
            );
            GridStyle.padding = new RectOffset(3, 4, 4, 3);

            StateStyles =
            [
                CreateStyle(
                    baseStyle: GUI.skin.button,
                    tex: TextureStore.GrayTexture,
                    col: Color.black
                ),
                CreateStyle(
                    baseStyle: GUI.skin.button,
                    tex: TextureStore.YellowTexture,
                    col: Color.black
                ),
                CreateStyle(
                    baseStyle: GUI.skin.button,
                    tex: TextureStore.RedTexture,
                    col: Color.black
                ),
                CreateStyle(
                    baseStyle: GUI.skin.button,
                    tex: TextureStore.GreenTexture,
                    col: Color.black
                ),
                CreateStyle(
                    baseStyle: GUI.skin.button,
                    tex: TextureStore.BlueTexture,
                    col: Color.black
                ),
            ];
        }

        internal static GUIStyle CreateStyle(
            GUIStyle baseStyle,
            Texture2D? tex = null,
            Texture2D? onTex = null,
            Color? col = null,
            Color? onCol = null
        )
        {
            GUIStyle style = new(baseStyle);
            CustomiseStyle(style, tex, onTex, col, onCol);
            // ensure the 2px border is preserved when Unity stretches the background.
            style.border = new RectOffset(2, 2, 2, 2);
            return style;
        }

        internal static void CustomiseStyle(
            GUIStyle style,
            Texture2D? tex = null,
            Texture2D? onTex = null,
            Color? col = null,
            Color? onCol = null
        )
        {
            if (tex is not null)
            {
                style.normal.background = tex;
                style.active.background = tex;
                style.focused.background = tex;
                style.hover.background = tex;
            }
            if (onTex is not null)
            {
                style.onNormal.background = onTex;
                style.onActive.background = onTex;
                style.onFocused.background = onTex;
                style.onHover.background = onTex;
            }
            if (col.HasValue)
            {
                style.normal.textColor = col.Value;
                style.hover.textColor = col.Value;
                style.focused.textColor = col.Value;
                style.active.textColor = col.Value;
            }
            if (onCol.HasValue)
            {
                style.onNormal.textColor = onCol.Value;
                style.onHover.textColor = onCol.Value;
                style.onFocused.textColor = onCol.Value;
                style.onActive.textColor = onCol.Value;
            }
        }

        internal static void AssignFontColors(GUIStyle style, Color col) { }

        internal static void AssignOnFontColors(GUIStyle style, Color col)
        {
            style.normal.textColor = col;
            style.hover.textColor = col;
            style.focused.textColor = col;
            style.active.textColor = col;
        }

        internal void ValidateStyles()
        {
            if (!TextureStore.ValidateTextures())
            {
                CustomiseStyle(
                    ToggleStyle,
                    tex: TextureStore.RedTexture,
                    onTex: TextureStore.GreenTexture
                );
                CustomiseStyle(
                    ToolbarStyle,
                    tex: TextureStore.GrayTexture,
                    onTex: TextureStore.RedTexture
                );
                CustomiseStyle(
                    GridStyle,
                    tex: TextureStore.GrayTexture,
                    onTex: TextureStore.RedTexture
                );
                CustomiseStyle(StateStyles[0], tex: TextureStore.GrayTexture);
                CustomiseStyle(StateStyles[1], tex: TextureStore.YellowTexture);
                CustomiseStyle(StateStyles[2], tex: TextureStore.RedTexture);
                CustomiseStyle(StateStyles[3], tex: TextureStore.GreenTexture);
                CustomiseStyle(StateStyles[4], tex: TextureStore.BlueTexture);
            }
        }

        internal GUIStyle ToggleStyle { get; }
        internal GUIStyle GroupStyle { get; }
        internal GUIStyle ToolbarStyle { get; }
        internal GUIStyle GridStyle { get; }
        internal GUIStyle[] StateStyles { get; }
        private GUIStyle MinimalPadding { get; }
        internal GUILayoutOption SliderLayout { get; } = GUILayout.Width(200f);
        internal GUILayoutOption WideSliderLayout { get; } = GUILayout.Width(400f);
        internal GUILayoutOption SliderLabelLayout { get; } = GUILayout.MinWidth(60f);

        internal int IncrementButton(
            in int oldValue,
            string label,
            in int max,
            params GUILayoutOption[] layout
        )
        {
            if (oldValue >= StateStyles.Length)
            {
                Main.Logger?.Error("IncrementButton index out of bounds.");
                return oldValue;
            }
            if (GUILayout.Button(label, StateStyles[oldValue], layout))
                return (oldValue + 1) % max;
            return oldValue;
        }

        internal float FloatHorizontalSlider(
            in float oldValue,
            in float min,
            in float max,
            in float? defaultValue = null,
            params GUILayoutOption[] layout
        )
        {
            if (layout.Length == 0)
                layout = [SliderLayout];
            bool reset = defaultValue.HasValue && GUILayout.Button("Reset", MinimalPadding);
            float newValue = 0;
            if (GUILayout.Button("-1", MinimalPadding))
                newValue -= 1f;
            if (GUILayout.Button("-0.1", MinimalPadding))
                newValue -= 0.1f;
            float sliderValue = GUILayout.HorizontalSlider(oldValue, min, max, layout);
            newValue += (float)Math.Round(sliderValue, 1);
            if (GUILayout.Button("+0.1", MinimalPadding))
                newValue += 0.1f;
            if (GUILayout.Button("+1", MinimalPadding))
                newValue += 1f;
            GUILayout.Space(10f);
            GUILayout.Label(oldValue.ToString("0.0"), SliderLabelLayout);
            return Mathf.Clamp(reset ? defaultValue!.Value : newValue, min, max);
        }

        internal int IntHorizontalSlider(
            in int oldValue,
            in int min,
            in int max,
            in int? defaultValue = null,
            params GUILayoutOption[] layout
        )
        {
            if (layout.Length == 0)
                layout = [SliderLayout];
            bool reset = defaultValue.HasValue && GUILayout.Button("Reset", MinimalPadding);
            int newValue = 0;
            if (GUILayout.Button("-1", MinimalPadding))
                newValue -= 1;
            float sliderValue = GUILayout.HorizontalSlider(oldValue, min, max, layout);
            newValue += Mathf.RoundToInt(sliderValue);
            if (GUILayout.Button("+1", MinimalPadding))
                newValue += 1;
            GUILayout.Space(10f);
            GUILayout.Label(oldValue.ToString("0.0"), SliderLabelLayout);
            return Mathf.Clamp(reset ? defaultValue!.Value : newValue, min, max);
        }

        internal bool SubtitleToggle(in string label, in bool oldValue)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, UnityModManager.UI.h2);
            GUILayout.Space(10);
            bool newValue = GUILayout.Toggle(
                oldValue,
                oldValue ? "  Show  " : "  Hide  ",
                ToolbarStyle
            );
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return newValue;
        }

        internal void TweakSectionLabel(in string label, in int indent = 0)
        {
            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            GUILayout.Space(20 * indent);
            GUILayout.Label(label);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        internal bool OnOffToggle(in bool oldValue)
        {
            return GUILayout.Toggle(oldValue, oldValue ? "  on  " : "  off  ", ToggleStyle);
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

            // Draw basic layout and title for the mod settings
            GUILayout.BeginVertical();

            MiningPatches.UI.OnGUI(Main.Settings.mineSettings, Context);
            NationPatches.UI.OnGUI(Main.Settings.nationSettings, Context);
            CombatPatches.UI.OnGUI(Main.Settings.combatSettings, Context);
            CouncilorPatches.UI.OnGUI(Main.Settings.councilorSettings, Context);

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
            CouncilorPatches.UI.OnHideGUI();
        }
    }
}
