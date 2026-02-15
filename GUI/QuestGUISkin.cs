using UnityEngine;

namespace QuestMod
{
    public static class QuestGUISkin
    {
        private static GUISkin skin;
        private static GUIStyle sectionHeader;
        private static GUIStyle tooltipStyle;
        private static Texture2D darkTex;
        private static Texture2D midTex;
        private static Texture2D lightTex;
        private static Texture2D accentTex;
        private static Texture2D hoverTex;
        private static Texture2D activeTex;
        private static Texture2D fieldTex;
        private static Texture2D greenTex;

        public static GUIStyle SectionHeader => sectionHeader;
        public static GUIStyle TooltipStyle => tooltipStyle;

        private static Texture2D MakeTex(Color col)
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { col, col, col, col });
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }

        public static GUISkin Get()
        {
            if (skin != null) return skin;

            darkTex = MakeTex(new Color(0.12f, 0.12f, 0.16f, 0.95f));
            midTex = MakeTex(new Color(0.18f, 0.18f, 0.22f, 0.95f));
            lightTex = MakeTex(new Color(0.25f, 0.25f, 0.30f, 0.95f));
            accentTex = MakeTex(new Color(0.30f, 0.30f, 0.38f, 1f));
            hoverTex = MakeTex(new Color(0.35f, 0.35f, 0.42f, 1f));
            activeTex = MakeTex(new Color(0.20f, 0.45f, 0.70f, 1f));
            fieldTex = MakeTex(new Color(0.10f, 0.10f, 0.14f, 1f));
            greenTex = MakeTex(new Color(0.15f, 0.40f, 0.25f, 1f));

            skin = ScriptableObject.CreateInstance<GUISkin>();
            skin.hideFlags = HideFlags.DontSave;

            skin.font = Font.CreateDynamicFontFromOSFont("Segoe UI", 13);

            skin.window = new GUIStyle(GUI.skin.window)
            {
                normal = { background = darkTex, textColor = Color.white },
                onNormal = { background = darkTex, textColor = Color.white },
                border = new RectOffset(8, 8, 20, 8),
                padding = new RectOffset(8, 8, 22, 8),
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };

            skin.label = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = new Color(0.90f, 0.90f, 0.92f) },
                fontSize = 13,
                padding = new RectOffset(2, 2, 1, 1)
            };

            skin.button = new GUIStyle(GUI.skin.button)
            {
                normal = { background = accentTex, textColor = Color.white },
                hover = { background = hoverTex, textColor = Color.white },
                active = { background = activeTex, textColor = Color.white },
                focused = { background = hoverTex, textColor = Color.white },
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                padding = new RectOffset(6, 6, 3, 3),
                margin = new RectOffset(2, 2, 2, 2),
                border = new RectOffset(4, 4, 4, 4)
            };

            skin.box = new GUIStyle(GUI.skin.box)
            {
                normal = { background = midTex, textColor = new Color(0.75f, 0.80f, 0.90f) },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(6, 6, 4, 4),
                margin = new RectOffset(2, 2, 2, 2),
                border = new RectOffset(2, 2, 2, 2),
                alignment = TextAnchor.MiddleCenter
            };

            skin.toggle = new GUIStyle(GUI.skin.toggle)
            {
                normal = { textColor = new Color(0.85f, 0.85f, 0.88f) },
                onNormal = { textColor = new Color(0.60f, 0.90f, 0.65f) },
                hover = { textColor = Color.white },
                onHover = { textColor = new Color(0.65f, 1f, 0.70f) },
                fontSize = 13,
                padding = new RectOffset(18, 2, 1, 1)
            };

            skin.textField = new GUIStyle(GUI.skin.textField)
            {
                normal = { background = fieldTex, textColor = Color.white },
                focused = { background = lightTex, textColor = Color.white },
                hover = { background = lightTex, textColor = Color.white },
                fontSize = 12,
                padding = new RectOffset(4, 4, 2, 2),
                border = new RectOffset(2, 2, 2, 2)
            };

            var tbNormal = new GUIStyle(skin.button)
            {
                normal = { background = lightTex, textColor = new Color(0.70f, 0.70f, 0.75f) },
                hover = { background = hoverTex, textColor = Color.white },
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 0, 0)
            };
            skin.customStyles = new[]
            {
                new GUIStyle(tbNormal) { name = "ToolbarButton" },
                new GUIStyle(tbNormal)
                {
                    name = "ToolbarButtonActive",
                    normal = { background = activeTex, textColor = Color.white },
                    fontStyle = FontStyle.Bold
                }
            };

            skin.horizontalSlider = new GUIStyle(GUI.skin.horizontalSlider)
            {
                normal = { background = midTex },
                fixedHeight = 12,
                margin = new RectOffset(4, 4, 6, 6)
            };
            skin.horizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                normal = { background = activeTex },
                hover = { background = hoverTex },
                fixedWidth = 14,
                fixedHeight = 14
            };

            skin.verticalScrollbar = new GUIStyle(GUI.skin.verticalScrollbar)
            {
                normal = { background = midTex },
                fixedWidth = 10
            };
            skin.verticalScrollbarThumb = new GUIStyle(GUI.skin.verticalScrollbarThumb)
            {
                normal = { background = lightTex }
            };

            sectionHeader = new GUIStyle(skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.65f, 0.75f, 0.90f) },
                padding = new RectOffset(0, 0, 4, 4),
                margin = new RectOffset(0, 0, 4, 2)
            };

            tooltipStyle = new GUIStyle(skin.box)
            {
                normal = { background = darkTex, textColor = new Color(0.95f, 0.95f, 0.80f) },
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 4, 4),
                wordWrap = true
            };

            return skin;
        }
    }
}
