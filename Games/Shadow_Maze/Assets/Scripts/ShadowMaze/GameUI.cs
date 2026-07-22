using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowMaze
{
    /// <summary>
    /// Builds and drives the whole on-screen interface at runtime: the "Step Forward" button,
    /// a status line, a red detection flash, and the victory screen that shows the 3D reward.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public event Action StepPressed;
        public event Action RestartPressed;

        private Font _font;
        private Text _status;
        private Image _flash;
        private GameObject _victoryPanel;
        private RawImage _beltImage;
        private Coroutine _flashRoutine;

        public void Build()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("GameCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            BuildTitle(canvasGo.transform);
            _status = BuildStatus(canvasGo.transform);
            BuildStepButton(canvasGo.transform);
            _flash = BuildFlash(canvasGo.transform);
            BuildVictory(canvasGo.transform);
        }

        public void SetStatus(string text, Color color)
        {
            _status.text = text;
            _status.color = color;
        }

        public void FlashAlert()
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(0.55f, 0f, t / 0.6f);
                _flash.color = new Color(1f, 0.05f, 0.05f, a);
                yield return null;
            }
            _flash.color = new Color(1f, 0f, 0f, 0f);
        }

        public void ShowVictory(RenderTexture beltTexture)
        {
            _beltImage.texture = beltTexture;
            _victoryPanel.SetActive(true);
        }

        public void HideVictory() => _victoryPanel.SetActive(false);

        // ----- builders -----

        private void BuildTitle(Transform parent)
        {
            var t = CreateText(parent, "Title", "SHADOW MAZE — Auris", 46, TextAnchor.UpperCenter,
                new Color(0.9f, 0.85f, 0.7f));
            Anchor(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(900, 70), new Vector2(0, -50));
        }

        private Text BuildStatus(Transform parent)
        {
            var t = CreateText(parent, "Status", "Крок вперед, коли світло йде вбік…", 30,
                TextAnchor.UpperCenter, Color.white);
            Anchor(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(1000, 60), new Vector2(0, -120));
            return t;
        }

        private void BuildStepButton(Transform parent)
        {
            var go = new GameObject("StepButton", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.15f, 0.5f, 0.35f, 0.95f);
            var rt = go.GetComponent<RectTransform>();
            Anchor(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(520, 150),
                new Vector2(0, 130));

            var label = CreateText(go.transform, "Label", "КРОК ВПЕРЕД  (W)", 40,
                TextAnchor.MiddleCenter, Color.white);
            Stretch(label.rectTransform);

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.2f, 0.65f, 0.45f);
            colors.pressedColor = new Color(0.1f, 0.35f, 0.25f);
            btn.colors = colors;
            btn.onClick.AddListener(() => StepPressed?.Invoke());
        }

        private Image BuildFlash(Transform parent)
        {
            var go = new GameObject("AlertFlash", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 0f, 0f, 0f);
            img.raycastTarget = false;
            Stretch(img.rectTransform);
            return img;
        }

        private void BuildVictory(Transform parent)
        {
            _victoryPanel = new GameObject("VictoryPanel", typeof(Image));
            _victoryPanel.transform.SetParent(parent, false);
            var bg = _victoryPanel.GetComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.05f, 0.92f);
            Stretch(bg.rectTransform);

            var win = CreateText(_victoryPanel.transform, "WinTitle", "ПЕРЕМОГА!", 64,
                TextAnchor.UpperCenter, new Color(1f, 0.85f, 0.35f));
            Anchor(win.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(900, 90), new Vector2(0, -180));

            var subtitle = CreateText(_victoryPanel.transform, "Subtitle",
                "Ауріс піднявся сходами", 34, TextAnchor.UpperCenter, Color.white);
            Anchor(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(900, 50), new Vector2(0, -280));

            var beltGo = new GameObject("BeltImage", typeof(RawImage));
            beltGo.transform.SetParent(_victoryPanel.transform, false);
            _beltImage = beltGo.GetComponent<RawImage>();
            Anchor(_beltImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(560, 560), new Vector2(0, 40));

            var reward = CreateText(_victoryPanel.transform, "Reward",
                "Нагорода: 3D-модель «Козацький Пояс»", 32, TextAnchor.MiddleCenter,
                new Color(1f, 0.8f, 0.4f));
            Anchor(reward.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(900, 60), new Vector2(0, -330));

            var btnGo = new GameObject("RestartButton", typeof(Image), typeof(Button));
            btnGo.transform.SetParent(_victoryPanel.transform, false);
            btnGo.GetComponent<Image>().color = new Color(0.3f, 0.25f, 0.1f, 0.95f);
            Anchor(btnGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(420, 120), new Vector2(0, 180));
            var btnLabel = CreateText(btnGo.transform, "Label", "ГРАТИ ЗНОВУ", 34,
                TextAnchor.MiddleCenter, Color.white);
            Stretch(btnLabel.rectTransform);
            btnGo.GetComponent<Button>().onClick.AddListener(() => RestartPressed?.Invoke());

            _victoryPanel.SetActive(false);
        }

        private Text CreateText(Transform parent, string name, string content, int size,
            TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.text = content;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
