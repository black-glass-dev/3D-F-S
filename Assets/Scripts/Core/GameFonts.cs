using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// Fonts bundled with the game. Hind Siliguri is an OFL (free) font that
    /// covers Bangla *and* Latin, so both languages render in the same style.
    /// Falls back to Unity's built-in font if a resource is missing.
    /// </summary>
    public static class GameFonts
    {
        private static Font _regular;
        private static Font _semiBold;
        private static Font _bold;
        private static Font _builtin;

        public static Font Regular
        {
            get
            {
                if (_regular == null) _regular = Load("Fonts/HindSiliguri-Regular");
                return _regular;
            }
        }

        public static Font SemiBold
        {
            get
            {
                if (_semiBold == null) _semiBold = Load("Fonts/HindSiliguri-SemiBold");
                return _semiBold;
            }
        }

        public static Font Bold
        {
            get
            {
                if (_bold == null) _bold = Load("Fonts/HindSiliguri-Bold");
                return _bold;
            }
        }

        public static Font Builtin
        {
            get
            {
                if (_builtin == null)
                {
                    _builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_builtin == null)
                        _builtin = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return _builtin;
            }
        }

        private static Font Load(string resourcePath)
        {
            // Dynamic fonts rasterise glyphs on demand, which is what Bangla
            // needs (thousands of glyphs plus conjuncts).
            var font = Resources.Load<Font>(resourcePath);
            if (font != null) return font;
            return Builtin;
        }
    }
}
