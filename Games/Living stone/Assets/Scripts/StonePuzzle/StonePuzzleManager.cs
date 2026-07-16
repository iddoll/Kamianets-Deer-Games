using System.Collections;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LivingStone.StonePuzzle
{
    /// <summary>
    /// Landscape puzzle: tap a hotspot on the wall sprite, then answer in a right side panel.
    /// </summary>
    public class StonePuzzleManager : MonoBehaviour
    {
        const string CorrectPassword = "ковпак";

        [Header("Hotspot")]
        [SerializeField] ClickableStone smoothStone;
        [SerializeField] AudioClip incorrectStoneClickClip;
        [SerializeField] AudioSource sfxSource;

        [Header("AR Overlay (on stone)")]
        [SerializeField] CanvasGroup hatOverlay;
        [SerializeField] float hatFadeDuration = 0.55f;

        [Header("Side Panel (slides from right)")]
        [SerializeField] RectTransform sidePanel;
        [SerializeField] CanvasGroup sidePanelGroup;
        [SerializeField] float panelSlideDuration = 0.45f;
        [SerializeField] TMP_InputField passwordInput;
        [SerializeField] Button submitButton;
        [SerializeField] RectTransform inputShakeTarget;
        [SerializeField] Image dragonPortrait;
        [SerializeField] TextMeshProUGUI hintLabel;
        [SerializeField] Color inputErrorColor = new Color(0.85f, 0.2f, 0.2f, 1f);
        [SerializeField] float shakeDuration = 0.35f;
        [SerializeField] float shakeStrength = 12f;

        [Header("Win")]
        [SerializeField] AudioClip doorOpenClip;
        [SerializeField] Image doorSuccessIcon;
        [SerializeField] CanvasGroup winOverlay;
        [SerializeField] float winFadeDuration = 0.5f;
        [SerializeField] UnityEvent onWin;

        bool _smoothStoneFound;
        bool _isWon;
        bool _panelOpen;
        Color _inputNormalColor;
        Coroutine _feedbackRoutine;
        Vector2 _panelHiddenPos;
        Vector2 _panelShownPos;

        public bool IsWon => _isWon;
        public bool SmoothStoneFound => _smoothStoneFound;

        void Awake()
        {
            if (sfxSource == null)
                sfxSource = GetComponent<AudioSource>();

            if (hatOverlay != null)
            {
                hatOverlay.alpha = 0f;
                hatOverlay.blocksRaycasts = false;
                hatOverlay.interactable = false;
                if (!hatOverlay.gameObject.activeSelf)
                    hatOverlay.gameObject.SetActive(true);
            }

            if (winOverlay != null)
            {
                winOverlay.alpha = 0f;
                winOverlay.blocksRaycasts = false;
                winOverlay.interactable = false;
                winOverlay.gameObject.SetActive(false);
            }

            if (sidePanel != null && sidePanelGroup == null)
                sidePanelGroup = sidePanel.GetComponent<CanvasGroup>();

            HideSidePanelImmediate();

            if (passwordInput != null)
                _inputNormalColor = passwordInput.colors.normalColor;

            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(OnSubmitPressed);
                submitButton.onClick.AddListener(OnSubmitPressed);
            }

            if (passwordInput != null)
                passwordInput.onSubmit.AddListener(_ => OnSubmitPressed());
        }

        void OnDestroy()
        {
            if (submitButton != null)
                submitButton.onClick.RemoveListener(OnSubmitPressed);

            if (passwordInput != null)
                passwordInput.onSubmit.RemoveAllListeners();
        }

        void HideSidePanelImmediate()
        {
            if (sidePanel == null)
                return;

            // Keep active so layout/rect width is valid, but park fully off-screen
            // and ignore clicks so the wall hotspots stay usable.
            sidePanel.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            CachePanelPositions();
            sidePanel.anchoredPosition = _panelHiddenPos;

            if (sidePanelGroup != null)
            {
                sidePanelGroup.alpha = 0f;
                sidePanelGroup.blocksRaycasts = false;
                sidePanelGroup.interactable = false;
            }

            _panelOpen = false;
        }

        void CachePanelPositions()
        {
            if (sidePanel == null)
                return;

            _panelShownPos = Vector2.zero;

            float width = sidePanel.rect.width;
            if (width < 10f)
            {
                var parent = sidePanel.parent as RectTransform;
                float parentW = parent != null ? parent.rect.width : 0f;
                // Panel is ~right 40% — push at least a full parent width off-screen.
                width = parentW > 10f ? parentW : Screen.width;
            }

            // Extra margin so nothing peeks into the play area.
            _panelHiddenPos = new Vector2(width + 80f, 0f);
        }

        public void OnStoneClicked(ClickableStone stone)
        {
            if (_isWon || stone == null)
                return;

            bool isCorrect = smoothStone != null
                ? stone == smoothStone
                : stone.IsSmoothStone;

            if (!isCorrect)
            {
                PlaySfx(incorrectStoneClickClip);
                return;
            }

            if (_smoothStoneFound)
                return;

            _smoothStoneFound = true;
            StartCoroutine(RevealHatAndSidePanel());
        }

        public void OnSubmitPressed()
        {
            if (_isWon || !_smoothStoneFound || !_panelOpen || passwordInput == null)
                return;

            string answer = passwordInput.text != null
                ? passwordInput.text.Trim()
                : string.Empty;

            if (string.Equals(answer, CorrectPassword, System.StringComparison.OrdinalIgnoreCase))
            {
                TriggerWin();
                return;
            }

            if (_feedbackRoutine != null)
                StopCoroutine(_feedbackRoutine);

            _feedbackRoutine = StartCoroutine(ShakeAndFlashInput());
        }

        IEnumerator RevealHatAndSidePanel()
        {
            if (hatOverlay != null)
            {
                float t = 0f;
                while (t < hatFadeDuration)
                {
                    t += Time.deltaTime;
                    hatOverlay.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, hatFadeDuration));
                    yield return null;
                }

                hatOverlay.alpha = 1f;
            }

            if (sidePanel != null)
            {
                sidePanel.gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                CachePanelPositions();
                sidePanel.anchoredPosition = _panelHiddenPos;

                if (sidePanelGroup != null)
                {
                    sidePanelGroup.alpha = 1f;
                    sidePanelGroup.blocksRaycasts = true;
                    sidePanelGroup.interactable = true;
                }

                float t = 0f;
                while (t < panelSlideDuration)
                {
                    t += Time.deltaTime;
                    float u = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.01f, panelSlideDuration));
                    sidePanel.anchoredPosition = Vector2.Lerp(_panelHiddenPos, _panelShownPos, u);
                    yield return null;
                }

                sidePanel.anchoredPosition = _panelShownPos;
            }

            _panelOpen = true;

            if (passwordInput != null)
            {
                passwordInput.text = string.Empty;
                passwordInput.ActivateInputField();
                passwordInput.Select();
            }
        }

        IEnumerator ShakeAndFlashInput()
        {
            var target = inputShakeTarget != null
                ? inputShakeTarget
                : passwordInput != null ? passwordInput.GetComponent<RectTransform>() : null;

            Vector2 origin = target != null ? target.anchoredPosition : Vector2.zero;
            bool hasColors = passwordInput != null;

            if (hasColors)
            {
                var colors = passwordInput.colors;
                colors.normalColor = inputErrorColor;
                colors.selectedColor = inputErrorColor;
                colors.highlightedColor = inputErrorColor;
                passwordInput.colors = colors;
            }

            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                if (target != null)
                {
                    float x = Mathf.Sin(elapsed * 40f) * shakeStrength;
                    target.anchoredPosition = origin + new Vector2(x, 0f);
                }

                yield return null;
            }

            if (target != null)
                target.anchoredPosition = origin;

            if (hasColors)
            {
                var colors = passwordInput.colors;
                colors.normalColor = _inputNormalColor;
                colors.selectedColor = _inputNormalColor;
                colors.highlightedColor = _inputNormalColor;
                passwordInput.colors = colors;
            }

            _feedbackRoutine = null;
        }

        void TriggerWin()
        {
            if (_isWon)
                return;

            _isWon = true;

            if (passwordInput != null)
                passwordInput.interactable = false;

            if (submitButton != null)
                submitButton.interactable = false;

            if (doorSuccessIcon != null)
                doorSuccessIcon.color = new Color(0.25f, 0.65f, 0.3f, 1f);

            PlaySfx(doorOpenClip);
            Debug.Log("Dungeon Opened");
            onWin?.Invoke();
            StartCoroutine(ShowWinOverlay());

#if UNITY_WEBGL && !UNITY_EDITOR
            NotifyHubCompleted();
#endif
        }

        IEnumerator ShowWinOverlay()
        {
            if (winOverlay == null)
                yield break;

            winOverlay.gameObject.SetActive(true);
            winOverlay.blocksRaycasts = true;
            winOverlay.interactable = true;

            float t = 0f;
            while (t < winFadeDuration)
            {
                t += Time.deltaTime;
                winOverlay.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, winFadeDuration));
                yield return null;
            }

            winOverlay.alpha = 1f;
        }

        void PlaySfx(AudioClip clip)
        {
            if (clip == null)
                return;

            if (sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
                return;
            }

            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void KamianetsDeerPostComplete();

        void NotifyHubCompleted()
        {
            KamianetsDeerPostComplete();
        }
#endif
    }
}
