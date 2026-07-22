using System.Collections.Generic;
using UnityEngine;

namespace ShadowMaze
{
    /// <summary>
    /// A searchlight fired from an arrow-slit (бійниця). It loops (ping-pong) through a set of
    /// grid cells along one row, switching cell every <see cref="switchInterval"/> seconds.
    /// Renders a moving light cone plus a bright focus cell. Turns red when it raises an alert.
    /// </summary>
    public class Searchlight : MonoBehaviour
    {
        public float switchInterval = 3f;

        private GridBoard _board;
        private List<Vector2Int> _patrol;
        private HashSet<Vector2Int> _blocked;
        private int _index;
        private int _dir = 1;
        private float _timer;
        private int _row;
        private bool _emitFromLeft;

        private SpriteRenderer _cellGlow;
        private SpriteRenderer _cone;
        private Transform _emitter;

        [Header("Colors (set by the bootstrap, tweakable per-light)")]
        public Color focusColor = new Color(1f, 0.93f, 0.55f, 0.85f);
        public Color coneColor = new Color(1f, 0.9f, 0.5f, 0.28f);
        public Color alertFocusColor = new Color(1f, 0.2f, 0.15f, 0.95f);
        public Color alertConeColor = new Color(1f, 0.15f, 0.1f, 0.45f);

        [Header("Moving light (the ghost that sweeps the cells)")]
        public Sprite lightSprite;
        [Range(0.4f, 1.4f)] public float lightScale = 0.96f;

        public Vector2Int CurrentCell => _patrol[_index];

        public void Initialize(GridBoard board, List<Vector2Int> patrol, float interval,
            int startIndex, int direction, bool emitFromLeft, HashSet<Vector2Int> blockedCells = null)
        {
            _board = board;
            _patrol = patrol;
            _blocked = blockedCells;
            switchInterval = interval;
            _index = Mathf.Clamp(startIndex, 0, patrol.Count - 1);
            _dir = direction >= 0 ? 1 : -1;
            _emitFromLeft = emitFromLeft;
            _row = patrol[0].y;
            _timer = 0f;

            BuildVisuals();
            RefreshVisuals();
        }

        public void ResetPatrol(int startIndex, int direction)
        {
            _index = Mathf.Clamp(startIndex, 0, _patrol.Count - 1);
            _dir = direction >= 0 ? 1 : -1;
            _timer = 0f;
            SetAlert(false);
            RefreshVisuals();
        }

        public void SetAlert(bool alert)
        {
            _cellGlow.color = alert ? alertFocusColor : focusColor;
            _cone.color = alert ? alertConeColor : coneColor;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < switchInterval) return;
            _timer -= switchInterval;
            Advance();
        }

        private void Advance()
        {
            if (_patrol.Count <= 1) return;

            int next = _index + _dir;
            if (!CanEnter(next))
            {
                // Barrel or patrol end ahead: turn back, just like hitting the path end.
                _dir = -_dir;
                next = _index + _dir;
                if (!CanEnter(next)) return; // boxed in between blockers: stay put this tick
            }

            _index = next;
            RefreshVisuals();
        }

        /// <summary>A cell is enterable if it is within the patrol and not occupied by a barrel.</summary>
        private bool CanEnter(int index)
        {
            if (index < 0 || index >= _patrol.Count) return false;
            if (_blocked != null && _blocked.Contains(_patrol[index])) return false;
            return true;
        }

        private void BuildVisuals()
        {
            var glowGo = new GameObject("Ghost");
            glowGo.transform.SetParent(transform, false);
            _cellGlow = glowGo.AddComponent<SpriteRenderer>();
            _cellGlow.sprite = lightSprite != null ? lightSprite : PrimitiveFactory.Ghost();
            _cellGlow.color = focusColor;
            _cellGlow.sortingOrder = 40;

            var coneGo = new GameObject("Cone");
            coneGo.transform.SetParent(transform, false);
            _cone = coneGo.AddComponent<SpriteRenderer>();
            _cone.sprite = PrimitiveFactory.Cone();
            _cone.color = coneColor;
            _cone.sortingOrder = 30;

            // The arrow-slit position on the side wall for this row (cone origin).
            int emitterX = _emitFromLeft ? -1 : _board.width;
            _emitter = new GameObject("ArrowSlit").transform;
            _emitter.SetParent(transform, false);
            _emitter.position = _board.CellToWorld(new Vector2Int(emitterX, _row), 0f);
        }

        private void RefreshVisuals()
        {
            Vector3 target = _board.CellToWorld(CurrentCell, 0f);
            _cellGlow.transform.position = new Vector3(target.x, target.y, 0f);
            _cellGlow.transform.localScale = new Vector3(lightScale, lightScale, 1f) * _board.cellSize;

            // Stretch the cone from the arrow-slit to the currently lit cell. The sprite's
            // pivot is on its apex (left edge); a negative X scale mirrors it for a right-side
            // slit so the apex stays pinned to the emitter either way.
            Vector3 from = _emitter.position;
            float length = Mathf.Abs(target.x - from.x) + _board.cellSize * 0.5f;
            _cone.transform.position = from;
            _cone.transform.rotation = Quaternion.identity;
            float signedLength = _emitFromLeft ? length : -length;
            _cone.transform.localScale = new Vector3(signedLength, _board.cellSize * 1.5f, 1f);
        }
    }
}
