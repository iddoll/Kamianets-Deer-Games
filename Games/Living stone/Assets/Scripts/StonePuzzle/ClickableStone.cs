using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LivingStone.StonePuzzle
{
    /// <summary>
    /// Invisible (or debug-outlined) hotspot on top of the wall sprite.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClickableStone : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] bool isSmoothStone;
        [SerializeField] Button button;
        [SerializeField] Image hotspotGraphic;
        [Tooltip("While true, hotspot shows a faint outline so you can place it on the wall art.")]
        [SerializeField] bool showDebugOutline = true;

        StonePuzzleManager _manager;

        public bool IsSmoothStone => isSmoothStone;

        void Awake()
        {
            EnsureButtonHook();
            ApplyDebugVisual();
        }

        public void Initialize(StonePuzzleManager manager, bool smoothStone)
        {
            _manager = manager;
            isSmoothStone = smoothStone;
            EnsureButtonHook();
            ApplyDebugVisual();
        }

        void EnsureButtonHook()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (hotspotGraphic == null)
                hotspotGraphic = GetComponent<Image>();

            if (button == null)
                return;

            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        void ApplyDebugVisual()
        {
            if (hotspotGraphic == null)
                return;

            // Transparent hit area; faint ring only for layout work.
            hotspotGraphic.color = showDebugOutline
                ? (isSmoothStone
                    ? new Color(0.9f, 0.75f, 0.35f, 0.35f)
                    : new Color(1f, 1f, 1f, 0.18f))
                : new Color(1f, 1f, 1f, 0f);
        }

        public void SetDebugOutline(bool visible)
        {
            showDebugOutline = visible;
            ApplyDebugVisual();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (button != null && button.interactable)
                return;

            HandleClick();
        }

        void HandleClick()
        {
            if (_manager == null)
                _manager = FindFirstObjectByType<StonePuzzleManager>();

            if (_manager == null)
            {
                Debug.LogWarning($"{name}: StonePuzzleManager not found.");
                return;
            }

            _manager.OnStoneClicked(this);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (hotspotGraphic == null)
                hotspotGraphic = GetComponent<Image>();
            ApplyDebugVisual();
        }
#endif
    }
}
