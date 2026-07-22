using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle
{
    /// <summary>
    /// Coordinates the anagram puzzle: validates drops against each slot's expected letter,
    /// returns wrong letters to their floating home, and fires the win sequence once the word is
    /// spelled — open chest, vibration, sparkle burst and the AR Silver Compass reward.
    /// </summary>
    public class WordPuzzleManager : MonoBehaviour
    {
        [HideInInspector] public Canvas canvas;
        [HideInInspector] public List<FloatingLetter> letters = new();
        [HideInInspector] public List<LetterSlot> slots = new();

        [HideInInspector] public Image chestImage;
        [HideInInspector] public Sprite chestClosed;
        [HideInInspector] public Sprite chestOpen;

        [HideInInspector] public SparkleBurst sparkle;
        [HideInInspector] public Vector2 chestCenter;

        [HideInInspector] public PuzzleAudio audioFx;
        [HideInInspector] public ARShowcase ar;

        [HideInInspector] public GameObject winBanner;
        [HideInInspector] public GameObject arOverlay;
        [HideInInspector] public RawImage arBackground;
        [HideInInspector] public RawImage compassImage;

        private Camera _cam;
        private bool _won;

        public void Begin()
        {
            _cam = canvas != null ? canvas.worldCamera : null;
            _won = false;
            if (chestImage != null && chestClosed != null) chestImage.sprite = chestClosed;
            if (winBanner != null) winBanner.SetActive(false);
            if (arOverlay != null) arOverlay.SetActive(false);
        }

        public void OnLetterPickedUp()
        {
            if (audioFx != null) audioFx.PlayPick();
        }

        /// <summary>Called by a letter when the drag ends; validates against the slot under the pointer.</summary>
        public void OnLetterReleased(FloatingLetter letter, Vector2 screenPosition)
        {
            if (_won) return;

            LetterSlot target = FindSlotUnder(screenPosition);
            if (target != null && target.Matches(letter))
            {
                letter.PlaceAt(target.CenterAnchoredPosition);
                target.Fill(letter);
                if (audioFx != null) audioFx.PlayCorrect();
                CheckWin();
            }
            else
            {
                letter.ReturnHome();
                if (audioFx != null) audioFx.PlayWrong();
            }
        }

        private LetterSlot FindSlotUnder(Vector2 screenPosition)
        {
            foreach (var slot in slots)
            {
                if (slot.Rect == null) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(slot.Rect, screenPosition, _cam))
                    return slot;
            }
            return null;
        }

        private void CheckWin()
        {
            foreach (var slot in slots)
                if (!slot.Occupied) return;

            StartCoroutine(WinSequence());
        }

        private IEnumerator WinSequence()
        {
            _won = true;

            // Open the chest.
            if (chestImage != null && chestOpen != null) chestImage.sprite = chestOpen;
            if (audioFx != null) audioFx.PlayChest();

            // Tactile feedback (no-op on platforms without a vibrator).
            Vibrate();

            // Sparkle/fireworks placeholder.
            if (sparkle != null)
            {
                sparkle.Play(chestCenter, 44);
                yield return new WaitForSeconds(0.15f);
                sparkle.Play(chestCenter + new Vector2(-120, 60), 24);
                sparkle.Play(chestCenter + new Vector2(120, 40), 24);
            }

            if (audioFx != null) audioFx.PlayWin();

            yield return new WaitForSeconds(0.5f);
            if (winBanner != null)
            {
                // Letters call SetAsLastSibling while dragging, so re-assert the banner on top.
                winBanner.transform.SetAsLastSibling();
                winBanner.SetActive(true);
            }
        }

        private static void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        // ---- AR overlay ----

        public void ShowAR()
        {
            if (arOverlay == null) return;
            arOverlay.transform.SetAsLastSibling();
            arOverlay.SetActive(true);

            if (ar != null)
            {
                var feed = ar.StartCamera();
                if (arBackground != null)
                {
                    if (feed != null)
                    {
                        arBackground.texture = feed;
                        arBackground.color = Color.white;
                    }
                    else
                    {
                        // No camera available: use a stylized AR-ish backdrop.
                        arBackground.texture = null;
                        arBackground.color = new Color(0.06f, 0.09f, 0.14f, 1f);
                    }
                }
                if (compassImage != null) compassImage.texture = ar.CompassTexture;
                ar.SetSpinning(true);
            }
        }

        public void HideAR()
        {
            if (arOverlay != null) arOverlay.SetActive(false);
            if (ar != null)
            {
                ar.SetSpinning(false);
                ar.StopCamera();
            }
        }

        public void Restart()
        {
            StopAllCoroutines();
            _won = false;

            foreach (var slot in slots) slot.Clear();
            foreach (var letter in letters) letter.ResetToHome();

            if (chestImage != null && chestClosed != null) chestImage.sprite = chestClosed;
            if (winBanner != null) winBanner.SetActive(false);
            HideAR();
        }
    }
}
