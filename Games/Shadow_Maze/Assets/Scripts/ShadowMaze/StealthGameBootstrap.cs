using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ShadowMaze
{
    /// <summary>
    /// The single entry point. Attach this to one empty GameObject in a scene (or use the
    /// Tools ▸ Shadow Maze menu) and press Play. It procedurally builds the entire top-down
    /// stealth game — grid room, Auris, barrels, stairs, three searchlights, UI, audio and the
    /// 3D reward — and wires the GameManager together. No manual scene setup or art assets needed.
    ///
    /// Everything generated is grouped under a child object named "_Generated" so it can be
    /// rebuilt or cleared in one step. Use the Inspector buttons (Build / Clear) to preview the
    /// layout in Edit Mode without entering Play — those objects persist in the scene and can be
    /// inspected and saved. Entering Play always rebuilds a fresh copy from the current settings.
    /// </summary>
    [DisallowMultipleComponent]
    public class StealthGameBootstrap : MonoBehaviour
    {
        public const string GeneratedRootName = "_Generated";

        [Header("Grid / Room")]
        [Min(3)] public int gridWidth = 9;
        [Min(3)] public int gridHeight = 13;
        public float cellSize = 1f;
        public int pathColumn = 4;

        [Header("Level Layout")]
        [Tooltip("Path node indices that are barrels (safe zones + checkpoints).")]
        public int[] barrelIndices = { 0, 4, 8 };

        [Header("Searchlights (one entry per ghost)")]
        [Tooltip("Configure each searchlight individually: row, patrol span, start cell, direction and speed.")]
        public SearchlightSetting[] searchlights =
        {
            new SearchlightSetting { row = 2,  minColumn = 2, maxColumn = 6, startColumn = 2, direction = 1,  switchInterval = 3f,   emitFromLeft = true },
            new SearchlightSetting { row = 6,  minColumn = 2, maxColumn = 6, startColumn = 4, direction = -1, switchInterval = 2.2f, emitFromLeft = false },
            new SearchlightSetting { row = 10, minColumn = 2, maxColumn = 6, startColumn = 6, direction = 1,  switchInterval = 3.6f, emitFromLeft = true },
        };

        [Header("Searchlight Appearance (shared)")]
        public Color lightColor = new Color(1f, 0.93f, 0.55f, 0.85f);
        public Color lightAlertColor = new Color(1f, 0.2f, 0.15f, 0.95f);
        [Tooltip("Sprite for the moving light/ghost that sweeps the cells (empty = generated ghost).")]
        public Sprite lightSprite;
        [Range(0.4f, 1.4f)] public float lightScale = 0.96f;

        [Header("Walls & Windows (castle perimeter)")]
        [Tooltip("Window sprite placed on the wall at each light's arrow-slit (empty = generated).")]
        public Sprite windowSprite;
        public Color windowTint = Color.white;
        [Tooltip("Stone wall sprite tiled around the field perimeter (empty = generated).")]
        public Sprite wallSprite;
        public Color wallTint = Color.white;

        [Header("Player (Auris)")]
        public Color playerColor = new Color(0.25f, 0.85f, 0.95f);
        [Range(0.2f, 1f)] public float playerScale = 0.62f;
        [Tooltip("Seconds for the smooth hop between nodes (visual only).")]
        public float playerMoveDuration = 0.14f;

        [Header("Barrels")]
        [Tooltip("How many cells left/right of the path the flanking barrels sit.")]
        [Min(1)] public int barrelSideOffset = 1;
        public Color barrelTint = Color.white;
        [Range(0.4f, 1.2f)] public float barrelScale = 0.9f;

        [Header("Stairs")]
        public Color stairsTint = Color.white;
        [Range(0.4f, 1.2f)] public float stairsScale = 0.98f;

        [Header("Appearance")]
        public Color floorColor = new Color(0.11f, 0.12f, 0.16f);
        public Color pathTileColor = new Color(0.20f, 0.22f, 0.28f);
        public Color gridLineColor = new Color(0.06f, 0.07f, 0.10f);
        public Color roomBackground = new Color(0.05f, 0.06f, 0.09f);

        [Header("Optional Sprite Overrides (leave empty to use generated art)")]
        public Sprite playerSprite;
        public Sprite barrelSprite;
        public Sprite stairsSprite;
        [Tooltip("If set, replaces the whole generated grid floor (stretched across the room).")]
        public Sprite floorSprite;

        private void Start() => BuildGame();

        /// <summary>Removes the current generated content, if any.</summary>
        public void ClearGenerated()
        {
            var existing = transform.Find(GeneratedRootName);
            if (existing == null) return;
            // DestroyImmediate (even in Play) so a rebuild in the same frame does not momentarily
            // see the old EventSystem / Canvas / GameManager, which would break UI input.
            DestroyImmediate(existing.gameObject);
        }

        /// <summary>Builds (or rebuilds) the whole game under the "_Generated" child object.</summary>
        public void BuildGame()
        {
            ClearGenerated();

            var rootGo = new GameObject(GeneratedRootName);
            rootGo.transform.SetParent(transform, false);
            var root = rootGo.transform;

            var board = rootGo.AddComponent<GridBoard>();
            board.Configure(gridWidth, gridHeight, cellSize);

            // --- Build the linear path up the chosen column ---
            var path = new List<Vector2Int>();
            for (int y = 0; y < gridHeight; y++)
                path.Add(new Vector2Int(pathColumn, y));
            int stairsIndex = path.Count - 1;

            var safeIndices = new HashSet<int>(barrelIndices);
            safeIndices.Add(0); // start is always safe

            // World cells occupied by barrels — used both for rendering and as ghost blockers.
            var barrelCells = ComputeBarrelCells(board, path, stairsIndex);

            BuildFloor(root, board, path, stairsIndex, barrelCells);
            BuildWalls(root, board);

            // --- Auris ---
            var player = CreatePlayer(root, board, path, safeIndices, stairsIndex);

            // --- Searchlights (each configured independently) ---
            int lightCount = searchlights != null ? searchlights.Length : 0;
            var lights = new Searchlight[lightCount];
            var startIndices = new int[lightCount];
            var startDirs = new int[lightCount];
            for (int i = 0; i < lightCount; i++)
            {
                var cfg = searchlights[i];

                int minX = Mathf.Clamp(Mathf.Min(cfg.minColumn, cfg.maxColumn), 0, gridWidth - 1);
                int maxX = Mathf.Clamp(Mathf.Max(cfg.minColumn, cfg.maxColumn), 0, gridWidth - 1);
                int row = Mathf.Clamp(cfg.row, 0, gridHeight - 1);

                var patrol = new List<Vector2Int>();
                for (int x = minX; x <= maxX; x++)
                    patrol.Add(new Vector2Int(x, row));

                int startIndex = Mathf.Clamp(cfg.startColumn, minX, maxX) - minX;
                int dir = cfg.direction >= 0 ? 1 : -1;
                startIndices[i] = startIndex;
                startDirs[i] = dir;

                var lightGo = new GameObject($"Searchlight_{i}");
                lightGo.transform.SetParent(root, false);
                var light = lightGo.AddComponent<Searchlight>();
                light.focusColor = lightColor;
                light.coneColor = WithAlpha(lightColor, lightColor.a * 0.33f);
                light.alertFocusColor = lightAlertColor;
                light.alertConeColor = WithAlpha(lightAlertColor, lightAlertColor.a * 0.47f);
                light.lightSprite = Or(lightSprite, PrimitiveFactory.Ghost());
                light.lightScale = lightScale;
                light.Initialize(board, patrol, cfg.switchInterval, startIndex, dir,
                    cfg.emitFromLeft, barrelCells);
                lights[i] = light;
            }

            // --- Audio, reward, UI, event system ---
            var audio = rootGo.AddComponent<ProceduralAudio>();
            var belt = BeltShowcase.Create(root);

            var uiGo = new GameObject("UI");
            uiGo.transform.SetParent(root, false);
            var ui = uiGo.AddComponent<GameUI>();
            ui.Build();

            EnsureEventSystem(root);
            ConfigureCamera();

            // --- Brain ---
            var manager = rootGo.AddComponent<GameManager>();
            manager.Initialize(player, lights, ui, audio, belt, startIndices, startDirs);
        }

        private HashSet<Vector2Int> ComputeBarrelCells(GridBoard board, List<Vector2Int> path,
            int stairsIndex)
        {
            var cells = new HashSet<Vector2Int>();
            foreach (int idx in barrelIndices)
            {
                if (idx < 0 || idx >= path.Count || idx == stairsIndex) continue;
                int row = path[idx].y;
                var left = new Vector2Int(pathColumn - barrelSideOffset, row);
                var right = new Vector2Int(pathColumn + barrelSideOffset, row);
                if (board.InBounds(left)) cells.Add(left);
                if (board.InBounds(right)) cells.Add(right);
            }
            return cells;
        }

        private void BuildFloor(Transform root, GridBoard board, List<Vector2Int> path,
            int stairsIndex, HashSet<Vector2Int> barrelCells)
        {
            var floorRoot = new GameObject("Floor").transform;
            floorRoot.SetParent(root, false);

            var pathCells = new HashSet<Vector2Int>(path);

            // The whole floor is a single crisp sprite (uniform squares, no sub-pixel gaps).
            var gridSprite = floorSprite != null
                ? floorSprite
                : PrimitiveFactory.GridFloor(gridWidth, gridHeight, 32, pathCells,
                    floorColor, pathTileColor, gridLineColor);
            var floorGo = CreateSprite(floorRoot, "FloorGrid", gridSprite, Vector3.zero,
                Color.white, 0);
            floorGo.transform.localScale = new Vector3(cellSize, cellSize, 1f);

            // Stairs marker.
            var stairs = CreateSprite(floorRoot, "Stairs", Or(stairsSprite, PrimitiveFactory.Stairs()),
                board.CellToWorld(path[stairsIndex]), stairsTint, 10);
            stairs.transform.localScale = new Vector3(stairsScale, stairsScale, 1f) * cellSize;

            // Barrels flank each safe node on the left and right, shielding Auris from side light.
            var barrelSpr = Or(barrelSprite, PrimitiveFactory.Barrel());
            foreach (var cell in barrelCells)
            {
                var barrel = CreateSprite(floorRoot, $"Barrel_{cell.x}_{cell.y}", barrelSpr,
                    board.CellToWorld(cell), barrelTint, 10);
                barrel.transform.localScale = new Vector3(barrelScale, barrelScale, 1f) * cellSize;
            }
        }

        /// <summary>
        /// Builds a one-cell-thick castle stone wall around the whole field, and places a window
        /// sprite on the wall at each searchlight's arrow-slit (the light originates there).
        /// </summary>
        private void BuildWalls(Transform root, GridBoard board)
        {
            var wallRoot = new GameObject("Walls").transform;
            wallRoot.SetParent(root, false);

            var wallSpr = Or(wallSprite, PrimitiveFactory.StoneWall());
            var windowSpr = Or(windowSprite, PrimitiveFactory.Window());

            // Which wall cells host a window (aligned with each light's emitter side and row).
            var windowCells = new HashSet<Vector2Int>();
            if (searchlights != null)
            {
                foreach (var cfg in searchlights)
                {
                    int wx = cfg.emitFromLeft ? -1 : gridWidth;
                    windowCells.Add(new Vector2Int(wx, Mathf.Clamp(cfg.row, 0, gridHeight - 1)));
                }
            }

            // Perimeter ring (corners included).
            for (int x = -1; x <= gridWidth; x++)
            {
                PlaceWall(wallRoot, board, wallSpr, windowSpr, windowCells, new Vector2Int(x, -1));
                PlaceWall(wallRoot, board, wallSpr, windowSpr, windowCells, new Vector2Int(x, gridHeight));
            }
            for (int y = 0; y < gridHeight; y++)
            {
                PlaceWall(wallRoot, board, wallSpr, windowSpr, windowCells, new Vector2Int(-1, y));
                PlaceWall(wallRoot, board, wallSpr, windowSpr, windowCells, new Vector2Int(gridWidth, y));
            }
        }

        private void PlaceWall(Transform parent, GridBoard board, Sprite wallSpr, Sprite windowSpr,
            HashSet<Vector2Int> windowCells, Vector2Int cell)
        {
            var pos = board.CellToWorld(cell);
            var wall = CreateSprite(parent, $"Wall_{cell.x}_{cell.y}", wallSpr, pos, wallTint, 8);
            wall.transform.localScale = new Vector3(cellSize, cellSize, 1f);

            if (windowCells.Contains(cell))
            {
                var window = CreateSprite(parent, $"Window_{cell.x}_{cell.y}", windowSpr, pos,
                    windowTint, 9);
                window.transform.localScale = new Vector3(0.92f, 0.92f, 1f) * cellSize;
            }
        }

        private PlayerController CreatePlayer(Transform root, GridBoard board, List<Vector2Int> path,
            HashSet<int> safeIndices, int stairsIndex)
        {
            var go = CreateSprite(root, "Auris", Or(playerSprite, PrimitiveFactory.Circle()),
                Vector3.zero, playerColor, 50);
            go.transform.localScale = new Vector3(playerScale, playerScale, 1f) * cellSize;

            // A small dark "cloak" ring behind Auris for readability.
            var ring = CreateSprite(go.transform, "Cloak", PrimitiveFactory.Circle(),
                Vector3.zero, new Color(0.05f, 0.2f, 0.25f, 0.8f), 49);
            ring.transform.localScale = Vector3.one * 1.35f;

            var player = go.AddComponent<PlayerController>();
            player.moveDuration = playerMoveDuration;
            player.Initialize(board, path, safeIndices, stairsIndex);
            return player;
        }

        private GameObject CreateSprite(Transform parent, string name, Sprite sprite, Vector3 pos,
            Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go;
        }

        private void EnsureEventSystem(Transform root)
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var esGo = new GameObject("EventSystem", typeof(EventSystem));
            esGo.transform.SetParent(root, false);
#if ENABLE_INPUT_SYSTEM
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#elif ENABLE_LEGACY_INPUT_MANAGER
            esGo.AddComponent<StandaloneInputModule>();
#endif
        }

        private void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = roomBackground;

            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 0.5625f;
            // +1.6 leaves room for the one-cell perimeter wall plus a small margin.
            float halfH = gridHeight * 0.5f + 1.8f;
            float halfW = gridWidth * 0.5f + 1.6f;
            cam.orthographicSize = Mathf.Max(halfH, halfW / Mathf.Max(0.1f, aspect));
        }

        private static Sprite Or(Sprite overrideSprite, Sprite fallback)
        {
            return overrideSprite != null ? overrideSprite : fallback;
        }

        private static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }
    }
}
