using UnityEngine;

namespace WordPuzzle
{
    /// <summary>
    /// Generates every sprite and material used by the puzzle at runtime, so the project needs
    /// no imported art. Sprites are authored on transparent backgrounds for clean UI usage.
    /// </summary>
    public static class PuzzleArt
    {
        private static Sprite _tile;
        private static Sprite _slot;
        private static Sprite _spark;
        private static Sprite _soft;
        private static Sprite _chestClosed;
        private static Sprite _chestOpen;

        /// <summary>Rounded-square tile (letters). Tint via Image.color.</summary>
        public static Sprite Tile() => _tile ??= RoundedSquare(128, 22, Color.white, default, 0);

        /// <summary>Rounded-square outline slot (empty target). Tint via Image.color.</summary>
        public static Sprite Slot() => _slot ??= RoundedSquare(128, 22, new Color(1, 1, 1, 0.12f),
            Color.white, 6);

        /// <summary>Four-point sparkle star for the win burst.</summary>
        public static Sprite Spark()
        {
            if (_spark != null) return _spark;
            int n = 64;
            var tex = NewTex(n);
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Abs(x - c) / c;
                float dy = Mathf.Abs(y - c) / c;
                // Star: strong along axes, fading diagonally.
                float axis = Mathf.Clamp01(1f - (dx * dx + dy) ) + Mathf.Clamp01(1f - (dy * dy + dx));
                float core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 1.4f);
                float a = Mathf.Clamp01(axis * 0.6f + core);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            _spark = ToSprite(tex);
            return _spark;
        }

        /// <summary>Soft radial dot (glow).</summary>
        public static Sprite Soft()
        {
            if (_soft != null) return _soft;
            int n = 64;
            var tex = NewTex(n);
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d)));
            }
            tex.Apply();
            _soft = ToSprite(tex);
            return _soft;
        }

        public static Sprite ChestClosed() => _chestClosed ??= BuildChest(false);
        public static Sprite ChestOpen() => _chestOpen ??= BuildChest(true);

        // ---------- builders ----------

        private static Sprite BuildChest(bool open)
        {
            int n = 256;
            var tex = NewTex(n);
            var wood = new Color(0.45f, 0.28f, 0.14f);
            var woodDark = new Color(0.32f, 0.19f, 0.09f);
            var metal = new Color(0.78f, 0.66f, 0.34f);
            var metalDark = new Color(0.5f, 0.4f, 0.18f);
            var interior = new Color(0.08f, 0.06f, 0.04f);
            var gold = new Color(1f, 0.85f, 0.35f);

            int left = 28, right = n - 28;
            int bottom = 30, top = n - 40;
            int seam = open ? (int)(bottom + (top - bottom) * 0.62f) : (int)(bottom + (top - bottom) * 0.55f);
            int lidTopClosed = top;

            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Color col = Color.clear;
                bool inBody = x >= left && x <= right && y >= bottom && y <= seam;
                if (inBody)
                {
                    col = ((x / 16) % 2 == 0) ? wood : woodDark;
                    if (x < left + 10 || x > right - 10) col = metal;          // side bands
                    if (Mathf.Abs(y - (bottom + seam) / 2) < 6) col = metalDark; // mid band
                }

                if (!open)
                {
                    // Rounded lid on top.
                    float lidCx = (left + right) * 0.5f;
                    float lidCy = seam;
                    float rx = (right - left) * 0.5f;
                    float ry = (lidTopClosed - seam);
                    if (y >= seam && y <= lidTopClosed)
                    {
                        float nx = (x - lidCx) / rx;
                        float ny = (y - lidCy) / ry;
                        if (nx * nx + ny * ny <= 1f)
                        {
                            col = ((x / 16) % 2 == 0) ? woodDark : wood;
                            if (Mathf.Abs(x - lidCx) < 8) col = metal; // center band
                        }
                    }
                    // Lock.
                    if (Mathf.Abs(x - lidCx) < 16 && y > seam - 20 && y < seam + 14)
                        col = metal;
                    if (Mathf.Abs(x - lidCx) < 6 && y > seam - 6 && y < seam + 6)
                        col = metalDark;
                }
                else
                {
                    // Open: interior + gold glow above the seam, small tilted lid at very top.
                    if (x >= left + 6 && x <= right - 6 && y > seam && y <= top)
                    {
                        float gx = (x - (left + right) * 0.5f) / ((right - left) * 0.5f);
                        float gy = (y - seam) / (float)(top - seam);
                        float glow = Mathf.Clamp01(1f - (gx * gx + (gy - 0.3f) * (gy - 0.3f)) * 1.3f);
                        col = Color.Lerp(interior, gold, glow);
                    }
                    // Lid flipped up as a thin bar near the top.
                    if (x >= left && x <= right && y >= top - 8 && y <= top + 8)
                        col = ((x / 16) % 2 == 0) ? woodDark : wood;
                }

                tex.SetPixel(x, y, col);
            }
            tex.Apply();
            return ToSprite(tex);
        }

        /// <summary>Rounded square with optional border. fill/border are colors; borderPx thickness.</summary>
        private static Sprite RoundedSquare(int n, int radius, Color fill, Color border, int borderPx)
        {
            var tex = NewTex(n);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float a = RoundedAlpha(x, y, n, radius);
                Color col = fill;
                if (borderPx > 0)
                {
                    float inner = RoundedAlpha(x, y, n, radius, borderPx);
                    if (a > 0.5f && inner < 0.5f) col = border; // ring
                }
                col.a *= a;
                tex.SetPixel(x, y, col);
            }
            tex.Apply();
            return ToSprite(tex);
        }

        private static float RoundedAlpha(int x, int y, int n, int radius, int inset = 0)
        {
            float lo = inset;
            float hi = n - 1 - inset;
            float r = radius;
            float cxLeft = lo + r, cxRight = hi - r, cyBot = lo + r, cyTop = hi - r;
            float px = Mathf.Clamp(x, cxLeft, cxRight);
            float py = Mathf.Clamp(y, cyBot, cyTop);
            float d = Mathf.Sqrt((x - px) * (x - px) + (y - py) * (y - py));
            if (x < lo || x > hi || y < lo || y > hi) return 0f;
            return Mathf.Clamp01(r - d + 0.5f) > 0 ? Mathf.Clamp01(r - d + 1f) : (d <= r ? 1f : 0f);
        }

        private static Texture2D NewTex(int n)
        {
            return new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        private static Sprite ToSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        /// <summary>URP-Lit (fallback Standard) material for the 3D reward.</summary>
        public static Material LitMaterial(Color color, float metallic, float smoothness, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (emission != Color.black)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission);
            }
            return mat;
        }
    }
}
