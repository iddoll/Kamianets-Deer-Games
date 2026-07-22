using UnityEngine;

namespace ShadowMaze
{
    /// <summary>
    /// Generates every sprite, texture and material used by the game at runtime so the
    /// project needs no imported art assets. All sprites are authored at 64 px = 1 world unit.
    /// </summary>
    public static class PrimitiveFactory
    {
        public const int Resolution = 64;
        public const float PixelsPerUnit = 64f;

        private static Sprite _square;
        private static Sprite _circle;
        private static Sprite _cone;
        private static Sprite _stairs;
        private static Sprite _barrel;
        private static Sprite _ghost;
        private static Sprite _window;
        private static Sprite _wall;

        /// <summary>Solid white square, tint via SpriteRenderer.color.</summary>
        public static Sprite Square()
        {
            if (_square != null) return _square;
            var tex = NewTexture();
            Fill(tex, Color.white);
            _square = MakeSprite(tex, new Vector2(0.5f, 0.5f));
            return _square;
        }

        /// <summary>Solid white circle (used for Auris and barrels).</summary>
        public static Sprite Circle()
        {
            if (_circle != null) return _circle;
            var tex = NewTexture();
            float c = (Resolution - 1) * 0.5f;
            float r = c - 1.5f;
            for (int y = 0; y < Resolution; y++)
            for (int x = 0; x < Resolution; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(r - d + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            _circle = MakeSprite(tex, new Vector2(0.5f, 0.5f));
            return _circle;
        }

        /// <summary>
        /// A light cone: apex at the left-center (the arrow-slit), widening to the right.
        /// Pivot sits on the apex so it can be scaled outward from an emitter point.
        /// </summary>
        public static Sprite Cone()
        {
            if (_cone != null) return _cone;
            var tex = NewTexture();
            float half = (Resolution - 1) * 0.5f;
            for (int x = 0; x < Resolution; x++)
            {
                float t = x / (float)(Resolution - 1);          // 0 at apex, 1 at base
                float spread = t * half;                        // half-height at this column
                float lengthFade = Mathf.Lerp(0.85f, 0.35f, t); // dimmer further from wall
                for (int y = 0; y < Resolution; y++)
                {
                    float dy = Mathf.Abs(y - half);
                    float edge = Mathf.Clamp01(spread - dy + 0.5f);
                    float sideFade = spread > 0.01f ? 1f - (dy / spread) * 0.6f : 0f;
                    float a = edge * lengthFade * Mathf.Clamp01(sideFade);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            _cone = MakeSprite(tex, new Vector2(0f, 0.5f));
            return _cone;
        }

        /// <summary>A barrel top: brown disc with staves and rim, drawn on transparent bg.</summary>
        public static Sprite Barrel()
        {
            if (_barrel != null) return _barrel;
            var tex = NewTexture();
            float c = (Resolution - 1) * 0.5f;
            float rOuter = c - 2f;
            float rRim = rOuter - 5f;
            var wood = new Color(0.45f, 0.28f, 0.13f);
            var woodDark = new Color(0.30f, 0.18f, 0.08f);
            var band = new Color(0.68f, 0.55f, 0.30f);
            for (int y = 0; y < Resolution; y++)
            for (int x = 0; x < Resolution; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                if (d > rOuter + 0.5f) { tex.SetPixel(x, y, Color.clear); continue; }
                Color col;
                if (d > rRim) col = band;                                   // metal band rim
                else col = ((x / 8) % 2 == 0) ? wood : woodDark;            // vertical staves
                float a = Mathf.Clamp01(rOuter - d + 0.5f);
                col.a = a;
                tex.SetPixel(x, y, col);
            }
            tex.Apply();
            _barrel = MakeSprite(tex, new Vector2(0.5f, 0.5f));
            return _barrel;
        }

        /// <summary>
        /// Default emitter art: a classic ghost silhouette (rounded head, wavy skirt, two eyes),
        /// tinted at runtime. Represents the spectral source behind each arrow-slit.
        /// </summary>
        public static Sprite Ghost()
        {
            if (_ghost != null) return _ghost;
            var tex = NewTexture();
            float cx = (Resolution - 1) * 0.5f;
            float r = cx - 6f;
            float headCy = Resolution * 0.60f;   // head center (y up)
            for (int y = 0; y < Resolution; y++)
            for (int x = 0; x < Resolution; x++)
            {
                float dx = x - cx;
                float a;
                if (y >= headCy)
                {
                    float d = Mathf.Sqrt(dx * dx + (y - headCy) * (y - headCy));
                    a = Mathf.Clamp01(r - d + 0.5f);                 // round head
                }
                else
                {
                    float wave = 8f + 4f * Mathf.Abs(Mathf.Sin(x * 0.45f)); // wavy skirt hem
                    float sideEdge = Mathf.Clamp01(r - Mathf.Abs(dx) + 0.5f);
                    float bottomEdge = Mathf.Clamp01(y - wave + 0.5f);
                    a = Mathf.Min(sideEdge, bottomEdge);
                }

                Color col = new Color(1f, 1f, 1f, a);

                // Two dark eyes.
                float eyeY = headCy - 2f;
                float eR = 3.4f;
                float dLeft = Mathf.Sqrt((x - (cx - 7f)) * (x - (cx - 7f)) + (y - eyeY) * (y - eyeY));
                float dRight = Mathf.Sqrt((x - (cx + 7f)) * (x - (cx + 7f)) + (y - eyeY) * (y - eyeY));
                if ((dLeft < eR || dRight < eR) && a > 0.5f)
                    col = new Color(0.1f, 0.1f, 0.15f, a);

                tex.SetPixel(x, y, col);
            }
            tex.Apply();
            _ghost = MakeSprite(tex, new Vector2(0.5f, 0.5f));
            return _ghost;
        }

        /// <summary>Placeholder window: stone frame with glass panes and a mullion cross.</summary>
        public static Sprite Window()
        {
            if (_window != null) return _window;
            var tex = NewTexture();
            var frame = new Color(0.32f, 0.30f, 0.28f);
            var glass = new Color(0.35f, 0.55f, 0.75f);
            var glassDim = new Color(0.22f, 0.38f, 0.55f);
            int border = 7;
            int barHalf = 3;
            int c = Resolution / 2;
            for (int y = 0; y < Resolution; y++)
            for (int x = 0; x < Resolution; x++)
            {
                bool inFrame = x < border || x >= Resolution - border ||
                               y < border || y >= Resolution - border;
                bool inBar = Mathf.Abs(x - c) < barHalf || Mathf.Abs(y - c) < barHalf;
                Color col;
                if (inFrame || inBar) col = frame;
                else col = ((x < c) ^ (y < c)) ? glass : glassDim;
                tex.SetPixel(x, y, col);
            }
            tex.Apply();
            _window = MakeSprite(tex, new Vector2(0.5f, 0.5f));
            return _window;
        }

        /// <summary>Placeholder castle stone wall: offset brick courses with mortar lines.</summary>
        public static Sprite StoneWall()
        {
            if (_wall != null) return _wall;
            var tex = NewTexture();
            var mortar = new Color(0.14f, 0.14f, 0.16f);
            var brickA = new Color(0.40f, 0.40f, 0.44f);
            var brickB = new Color(0.34f, 0.34f, 0.38f);
            int brickH = 16;
            int brickW = 32;
            int mortarPx = 2;
            for (int y = 0; y < Resolution; y++)
            {
                int row = y / brickH;
                int offset = (row % 2 == 0) ? 0 : brickW / 2;
                for (int x = 0; x < Resolution; x++)
                {
                    int localY = y % brickH;
                    int localX = (x + offset) % brickW;
                    bool isMortar = localY < mortarPx || localX < mortarPx;
                    Color col = isMortar ? mortar : (((x + offset) / brickW + row) % 2 == 0 ? brickA : brickB);
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            _wall = MakeSprite(tex, new Vector2(0.5f, 0.5f));
            return _wall;
        }

        /// <summary>Stairs tile: golden field with darker step lines.</summary>
        public static Sprite Stairs()
        {
            if (_stairs != null) return _stairs;
            var tex = NewTexture();
            var baseCol = new Color(0.85f, 0.72f, 0.30f);
            var lineCol = new Color(0.55f, 0.42f, 0.12f);
            for (int y = 0; y < Resolution; y++)
            for (int x = 0; x < Resolution; x++)
            {
                bool line = (y % 14) < 3;
                tex.SetPixel(x, y, line ? lineCol : baseCol);
            }
            tex.Apply();
            _stairs = MakeSprite(tex, new Vector2(0.5f, 0.5f));
            return _stairs;
        }

        /// <summary>
        /// Builds the whole room floor as ONE crisp texture (1 cell = <paramref name="cellPx"/> px,
        /// authored so 1 cell = 1 world unit). Using a single sprite avoids the sub-pixel gaps that
        /// make a grid of separate tile sprites look uneven. Path cells get their own tint.
        /// </summary>
        public static Sprite GridFloor(int width, int height, int cellPx,
            System.Collections.Generic.HashSet<Vector2Int> pathCells,
            Color floor, Color path, Color line)
        {
            int w = width * cellPx;
            int h = height * cellPx;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int cx = x / cellPx;
                int cy = y / cellPx;
                int lx = x - cx * cellPx;
                int ly = y - cy * cellPx;
                Color c = pathCells.Contains(new Vector2Int(cx, cy)) ? path : floor;
                bool onLine = lx == 0 || ly == 0 || lx >= cellPx - 1 || ly >= cellPx - 1;
                if (onLine) c = line;
                px[y * w + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), cellPx,
                0, SpriteMeshType.FullRect);
        }

        public static Sprite MakeSprite(Texture2D tex, Vector2 pivot)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, PixelsPerUnit,
                0, SpriteMeshType.FullRect);
        }

        private static Texture2D NewTexture()
        {
            var tex = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            return tex;
        }

        private static void Fill(Texture2D tex, Color c)
        {
            var pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
            tex.SetPixels(pixels);
            tex.Apply();
        }

        /// <summary>Creates a URP-Lit material for the 3D reward, falling back to Standard.</summary>
        public static Material LitMaterial(Color color, float metallic, float smoothness, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
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
