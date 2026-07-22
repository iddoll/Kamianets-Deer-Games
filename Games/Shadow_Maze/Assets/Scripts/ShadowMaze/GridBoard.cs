using UnityEngine;

namespace ShadowMaze
{
    /// <summary>
    /// Owns the top-down grid geometry. Converts grid cells (Vector2Int) into centered
    /// world positions so every other system can talk purely in cell coordinates.
    /// </summary>
    public class GridBoard : MonoBehaviour
    {
        [Min(1)] public int width = 9;
        [Min(1)] public int height = 13;
        public float cellSize = 1f;

        /// <summary>World position of cell (0,0). Chosen so the whole grid is centered on origin.</summary>
        public Vector2 Origin { get; private set; }

        public void Configure(int w, int h, float cell)
        {
            width = w;
            height = h;
            cellSize = cell;
            RecalculateOrigin();
        }

        private void Awake() => RecalculateOrigin();

        public void RecalculateOrigin()
        {
            float ox = -((width - 1) * 0.5f) * cellSize;
            float oy = -((height - 1) * 0.5f) * cellSize;
            Origin = new Vector2(ox, oy);
        }

        public Vector3 CellToWorld(Vector2Int cell, float z = 0f)
        {
            return new Vector3(Origin.x + cell.x * cellSize, Origin.y + cell.y * cellSize, z);
        }

        public bool InBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
        }
    }
}
