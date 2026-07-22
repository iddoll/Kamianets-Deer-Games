using System;

namespace ShadowMaze
{
    /// <summary>
    /// Per-searchlight configuration edited in the Inspector. Each ghost has its own row,
    /// patrol span, starting cell (which sets the initial beam length), direction, wall side
    /// and speed, so lights can behave completely independently.
    /// </summary>
    [Serializable]
    public class SearchlightSetting
    {
        [UnityEngine.Tooltip("Grid row (Y) this searchlight sweeps.")]
        public int row = 2;

        [UnityEngine.Tooltip("Leftmost patrol column (inclusive).")]
        public int minColumn = 2;

        [UnityEngine.Tooltip("Rightmost patrol column (inclusive).")]
        public int maxColumn = 6;

        [UnityEngine.Tooltip("Column where the ghost starts. Near the window = short beam; " +
            "near the path = long beam.")]
        public int startColumn = 2;

        [UnityEngine.Tooltip("Initial move direction: +1 = towards higher columns, -1 = lower.")]
        public int direction = 1;

        [UnityEngine.Tooltip("Seconds between cell steps for THIS light (its own speed).")]
        public float switchInterval = 3f;

        [UnityEngine.Tooltip("Window/beam on the LEFT wall (unchecked = right wall).")]
        public bool emitFromLeft = true;
    }
}
