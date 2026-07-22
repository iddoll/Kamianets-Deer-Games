using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowMaze
{
    /// <summary>
    /// Controls 'Auris'. Moves one node forward along a predefined linear path each time
    /// StepForward() is called. Tracks the last visited safe zone (barrel) as a checkpoint
    /// and can reset back to it. The logical cell updates instantly on a step; the transform
    /// smoothly interpolates for visual feedback only.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public float moveDuration = 0.14f;

        private GridBoard _board;
        private List<Vector2Int> _path;
        private HashSet<int> _safeIndices;
        private int _stairsIndex;

        private int _currentIndex;
        private int _lastSafeIndex;
        private Vector3 _fromPos, _toPos;
        private float _moveT = 1f;

        public event Action ReachedStairs;

        public int CurrentIndex => _currentIndex;
        public Vector2Int CurrentCell => _path[_currentIndex];
        public bool IsOnSafeZone => _safeIndices.Contains(_currentIndex);
        public bool IsMoving => _moveT < 1f;
        public bool HasReachedStairs => _currentIndex >= _stairsIndex;

        public void Initialize(GridBoard board, List<Vector2Int> path, HashSet<int> safeIndices, int stairsIndex)
        {
            _board = board;
            _path = path;
            _safeIndices = safeIndices;
            _stairsIndex = stairsIndex;
            _currentIndex = 0;
            _lastSafeIndex = _safeIndices.Contains(0) ? 0 : 0;
            SnapToCurrent();
        }

        /// <summary>Advance one node toward the stairs. Returns true if a step happened.</summary>
        public bool StepForward()
        {
            if (HasReachedStairs) return false;

            _currentIndex = Mathf.Min(_currentIndex + 1, _path.Count - 1);
            if (IsOnSafeZone) _lastSafeIndex = _currentIndex;

            BeginMoveTo(_currentIndex);

            if (_currentIndex >= _stairsIndex)
                ReachedStairs?.Invoke();

            return true;
        }

        /// <summary>Detected outside a safe zone: teleport back to the last barrel checkpoint.</summary>
        public void ResetToCheckpoint()
        {
            _currentIndex = _lastSafeIndex;
            SnapToCurrent();
        }

        public void ResetToStart()
        {
            _currentIndex = 0;
            _lastSafeIndex = 0;
            SnapToCurrent();
        }

        private void BeginMoveTo(int index)
        {
            _fromPos = transform.position;
            _toPos = _board.CellToWorld(_path[index], transform.position.z);
            _moveT = 0f;
        }

        private void SnapToCurrent()
        {
            _toPos = _board.CellToWorld(_path[_currentIndex], transform.position.z);
            transform.position = _toPos;
            _moveT = 1f;
        }

        private void Update()
        {
            if (_moveT >= 1f) return;
            _moveT += Time.deltaTime / Mathf.Max(0.01f, moveDuration);
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_moveT));
            transform.position = Vector3.Lerp(_fromPos, _toPos, e);
        }
    }
}
