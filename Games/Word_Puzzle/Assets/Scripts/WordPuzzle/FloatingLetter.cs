using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WordPuzzle
{
    /// <summary>
    /// A magical letter that orbits around the chest on an elliptical path (with a subtle radius
    /// wobble) and can be dragged into a slot. If dropped on the correct slot it snaps to the
    /// center; otherwise it flies back onto its orbit and keeps circling.
    /// </summary>
    public class FloatingLetter : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public char Character { get; private set; }
        public bool Placed { get; private set; }

        private RectTransform _rt;
        private RectTransform _parent;
        private Canvas _canvas;
        private Camera _cam;
        private WordPuzzleManager _manager;

        private Vector2 _center;      // chest center (orbit pivot)
        private float _rx, _ry;       // orbit ellipse radii
        private float _angSpeed;      // radians / second
        private float _angle;         // current orbit angle (radians)
        private float _startAngle;
        private float _wobbleAmp, _wobbleSpeed, _wobbleT, _wobblePhase;

        private bool _dragging;
        private bool _animating;

        public void Initialize(WordPuzzleManager manager, Canvas canvas, RectTransform parent,
            char character, Vector2 center, float radiusX, float radiusY, float angularSpeedDeg,
            float startAngleDeg, float wobbleAmp, float wobbleSpeed, float wobblePhase)
        {
            _manager = manager;
            _canvas = canvas;
            _cam = canvas.worldCamera; // null for Screen Space - Overlay
            _parent = parent;
            _rt = (RectTransform)transform;
            Character = character;
            _center = center;
            _rx = radiusX;
            _ry = radiusY;
            _angSpeed = angularSpeedDeg * Mathf.Deg2Rad;
            _startAngle = startAngleDeg * Mathf.Deg2Rad;
            _angle = _startAngle;
            _wobbleAmp = wobbleAmp;
            _wobbleSpeed = wobbleSpeed;
            _wobblePhase = wobblePhase;
            _rt.anchoredPosition = OrbitPosition();
        }

        private Vector2 OrbitPosition()
        {
            float wob = Mathf.Sin(_wobbleT + _wobblePhase) * _wobbleAmp;
            return _center + new Vector2(
                Mathf.Cos(_angle) * (_rx + wob),
                Mathf.Sin(_angle) * (_ry + wob));
        }

        private void Update()
        {
            if (Placed || _dragging || _animating) return;
            float dt = Time.deltaTime;
            _angle += _angSpeed * dt;
            _wobbleT += dt * _wobbleSpeed;
            _rt.anchoredPosition = OrbitPosition();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Placed) return;
            _dragging = true;
            _animating = false;
            _rt.SetAsLastSibling();
            _rt.localScale = Vector3.one * 1.1f;
            if (_manager != null) _manager.OnLetterPickedUp();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Placed || !_dragging) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parent, eventData.position, _cam, out var local))
                _rt.anchoredPosition = local;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Placed) return;
            _dragging = false;
            _rt.localScale = Vector3.one;
            if (_manager != null) _manager.OnLetterReleased(this, eventData.position);
        }

        /// <summary>Correct drop: snap smoothly to the slot center and lock in place.</summary>
        public void PlaceAt(Vector2 anchoredPos)
        {
            Placed = true;
            _dragging = false;
            StopAllCoroutines();
            StartCoroutine(MoveTo(anchoredPos, 0.12f, null));
        }

        /// <summary>Wrong drop: fly back onto the orbit and resume circling.</summary>
        public void ReturnHome()
        {
            StopAllCoroutines();
            StartCoroutine(MoveTo(OrbitPosition(), 0.2f, null));
        }

        public void ResetToHome()
        {
            Placed = false;
            _dragging = false;
            StopAllCoroutines();
            _animating = false;
            _angle = _startAngle;
            _wobbleT = 0f;
            _rt.localScale = Vector3.one;
            _rt.anchoredPosition = OrbitPosition();
        }

        private IEnumerator MoveTo(Vector2 target, float duration, System.Action onDone)
        {
            _animating = true;
            Vector2 from = _rt.anchoredPosition;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, duration);
                _rt.anchoredPosition = Vector2.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            _rt.anchoredPosition = target;
            _animating = false;
            onDone?.Invoke();
        }
    }
}
