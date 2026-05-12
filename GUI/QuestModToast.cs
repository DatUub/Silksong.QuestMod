using System.Collections.Generic;
using UnityEngine;

namespace QuestMod
{
    // On-screen toast queue. Upper-right corner, fades out. Always visible.
    public static class QuestModToast
    {
        private struct Entry
        {
            public string Msg;
            public float Expiry;
            public Color Tint;
        }

        private const float DefaultDurationSec = 5f;
        private static readonly List<Entry> _entries = new();
        private static GUIStyle _boxStyle;
        private static readonly object _lock = new();

        public static void Show(string msg, float durationSec = DefaultDurationSec)
        {
            Show(msg, Color.white, durationSec);
        }

        public static void Show(string msg, Color tint, float durationSec = DefaultDurationSec)
        {
            if (string.IsNullOrEmpty(msg)) return;
            lock (_lock)
            {
                _entries.Add(new Entry
                {
                    Msg = msg,
                    Expiry = Time.realtimeSinceStartup + durationSec,
                    Tint = tint,
                });
                if (_entries.Count > 8)
                    _entries.RemoveAt(0);
            }
        }

        public static void DrawAll()
        {
            lock (_lock)
            {
                if (_entries.Count == 0) return;
                float now = Time.realtimeSinceStartup;
                _entries.RemoveAll(e => e.Expiry < now);
                if (_entries.Count == 0) return;

                if (_boxStyle == null)
                {
                    _boxStyle = new GUIStyle(GUI.skin.box)
                    {
                        fontSize = 14,
                        alignment = TextAnchor.MiddleLeft,
                        wordWrap = true,
                        padding = new RectOffset(10, 10, 6, 6),
                    };
                }

                float maxWidth = 380f;
                float screenWidth = Screen.width;
                float y = 24f;

                foreach (var e in _entries)
                {
                    var content = new GUIContent(e.Msg);
                    var size = _boxStyle.CalcSize(content);
                    float w = Mathf.Min(size.x + 24f, maxWidth);
                    var rect = new Rect(screenWidth - w - 24f, y, w, _boxStyle.CalcHeight(content, w));

                    // Fade over last 1.5s.
                    float remaining = e.Expiry - now;
                    float alpha = Mathf.Clamp01(remaining / 1.5f); // last 1.5s fades

                    var prev = GUI.color;
                    GUI.color = new Color(e.Tint.r, e.Tint.g, e.Tint.b, alpha);
                    GUI.Box(rect, content, _boxStyle);
                    GUI.color = prev;

                    y += rect.height + 6f;
                }
            }
        }
    }
}
