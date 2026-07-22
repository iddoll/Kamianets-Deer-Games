using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WordPuzzle
{
    /// <summary>
    /// Single entry point. Attach to one empty GameObject (or use the Tools ▸ Word Puzzle menu)
    /// and press Play. It procedurally builds the whole Drag-and-Drop anagram game — chest,
    /// floating letters, target slots, UI, audio, sparkle FX and the AR Silver Compass reward —
    /// and wires the manager together. No manual scene setup or art assets required.
    /// </summary>
    [DisallowMultipleComponent]
    public class WordPuzzleBootstrap : MonoBehaviour
    {
        public const string GeneratedRootName = "_Generated";

        [Header("Puzzle")]
        [Tooltip("Target word to spell (one draggable letter + one slot per character).")]
        public string targetWord = "ВОЛЯ";

        [Header("Layout (reference resolution 1080 x 1920)")]
        public Vector2 chestPosition = new Vector2(0f, 360f);
        public Vector2 chestSize = new Vector2(470f, 380f);
        public float slotSize = 185f;
        public float slotY = -280f;
        public float slotSpacing = 210f;
        public float letterSize = 150f;

        [Header("Orbit Motion (letters circle the chest)")]
        public float orbitRadiusX = 370f;
        public float orbitRadiusY = 250f;
        [Tooltip("Orbit speed in degrees per second (negative = reverse direction).")]
        public float orbitSpeed = 35f;
        [Tooltip("Subtle in/out breathing of the orbit radius.")]
        public float wobbleAmount = 18f;
        public float wobbleSpeed = 1.6f;

        [Header("Colors")]
        public Color background = new Color(0.09f, 0.07f, 0.16f);
        public Color glowColor = new Color(0.5f, 0.35f, 0.9f, 0.5f);
        public Color letterColor = new Color(0.55f, 0.35f, 0.9f);
        public Color letterTextColor = Color.white;
        public Color slotColor = new Color(1f, 1f, 1f, 0.9f);
        public Color titleColor = new Color(0.95f, 0.9f, 0.7f);

        [Header("Optional Sprite Overrides (empty = generated art)")]
        public Sprite chestClosedSprite;
        public Sprite chestOpenSprite;
        public Sprite letterTileSprite;
        public Sprite slotSprite;

        private Font _font;

        private void Start() => BuildGame();

        public void ClearGenerated()
        {
            var existing = transform.Find(GeneratedRootName);
            if (existing != null) DestroyImmediate(existing.gameObject);
        }

        public void BuildGame()
        {
            ClearGenerated();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            string word = string.IsNullOrEmpty(targetWord) ? "ВОЛЯ" : targetWord.Trim();
            int count = word.Length;

            var rootGo = new GameObject(GeneratedRootName);
            rootGo.transform.SetParent(transform, false);
            var root = rootGo.transform;

            ConfigureCamera();

            // --- Canvas ---
            var canvasGo = new GameObject("PuzzleCanvas");
            canvasGo.transform.SetParent(root, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var canvasRect = (RectTransform)canvasGo.transform;

            // --- Background & glow ---
            var bg = CreateImage(canvasRect, "Background", PuzzleArt.Soft(), background);
            Stretch(bg.rectTransform);
            bg.sprite = null; // flat color fill
            bg.raycastTarget = false;

            var glow = CreateImage(canvasRect, "Glow", PuzzleArt.Soft(), glowColor);
            Center(glow.rectTransform, new Vector2(760, 760), chestPosition);
            glow.raycastTarget = false;

            // --- Title ---
            var title = CreateText(canvasRect, "Title", "Склади магічне слово", 46,
                TextAnchor.UpperCenter, titleColor);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(900, 70),
                new Vector2(0, -60));

            // --- Chest ---
            var chest = CreateImage(canvasRect, "Chest",
                chestClosedSprite != null ? chestClosedSprite : PuzzleArt.ChestClosed(), Color.white);
            Center(chest.rectTransform, chestSize, chestPosition);
            chest.raycastTarget = false;

            // --- Manager (created early so children can reference it) ---
            var manager = rootGo.AddComponent<WordPuzzleManager>();
            manager.canvas = canvas;

            // --- Slots ---
            var slots = new List<LetterSlot>();
            float startX = -((count - 1) * 0.5f) * slotSpacing;
            var slotSpr = slotSprite != null ? slotSprite : PuzzleArt.Slot();
            for (int i = 0; i < count; i++)
            {
                var img = CreateImage(canvasRect, $"Slot_{i}", slotSpr, slotColor);
                Center(img.rectTransform, new Vector2(slotSize, slotSize),
                    new Vector2(startX + i * slotSpacing, slotY));
                img.raycastTarget = false;
                var slot = img.gameObject.AddComponent<LetterSlot>();
                slot.Initialize(i, word[i]);
                slots.Add(slot);
            }

            // --- Letters (shuffled homes around the chest) ---
            var chars = new List<char>(word.ToCharArray());
            Shuffle(chars);
            var letters = new List<FloatingLetter>();
            var tileSpr = letterTileSprite != null ? letterTileSprite : PuzzleArt.Tile();
            for (int i = 0; i < chars.Count; i++)
            {
                // Evenly distribute the letters around the orbit.
                float startAngleDeg = (i / (float)chars.Count) * 360f + 25f;
                float rad = startAngleDeg * Mathf.Deg2Rad;
                Vector2 startPos = chestPosition + new Vector2(
                    Mathf.Cos(rad) * orbitRadiusX,
                    Mathf.Sin(rad) * orbitRadiusY);

                var img = CreateImage(canvasRect, $"Letter_{i}", tileSpr, letterColor);
                Center(img.rectTransform, new Vector2(letterSize, letterSize), startPos);
                img.raycastTarget = true;

                var label = CreateText(img.rectTransform, "Char", chars[i].ToString(), 92,
                    TextAnchor.MiddleCenter, letterTextColor);
                Stretch(label.rectTransform);
                label.fontStyle = FontStyle.Bold;

                var letter = img.gameObject.AddComponent<FloatingLetter>();
                letter.Initialize(manager, canvas, canvasRect, chars[i], chestPosition,
                    orbitRadiusX, orbitRadiusY, orbitSpeed, startAngleDeg,
                    wobbleAmount, wobbleSpeed, i * 1.7f);
                letters.Add(letter);
            }

            // --- Sparkle layer ---
            var sparkleGo = new GameObject("Sparkles", typeof(RectTransform));
            sparkleGo.transform.SetParent(canvasRect, false);
            Stretch((RectTransform)sparkleGo.transform);
            var sparkle = sparkleGo.AddComponent<SparkleBurst>();

            // --- Win banner ---
            var winBanner = BuildWinBanner(canvasRect, manager);

            // --- AR overlay + showcase ---
            var ar = ARShowcase.Create(root);
            var arOverlay = BuildAROverlay(canvasRect, manager, out var arBackground, out var compassImage);

            // --- Audio & event system ---
            var audioFx = rootGo.AddComponent<PuzzleAudio>();
            EnsureEventSystem(root);

            // --- Wire manager ---
            manager.letters = letters;
            manager.slots = slots;
            manager.chestImage = chest;
            manager.chestClosed = chestClosedSprite != null ? chestClosedSprite : PuzzleArt.ChestClosed();
            manager.chestOpen = chestOpenSprite != null ? chestOpenSprite : PuzzleArt.ChestOpen();
            manager.sparkle = sparkle;
            manager.chestCenter = chestPosition;
            manager.audioFx = audioFx;
            manager.ar = ar;
            manager.winBanner = winBanner;
            manager.arOverlay = arOverlay;
            manager.arBackground = arBackground;
            manager.compassImage = compassImage;
            manager.Begin();
        }

        private GameObject BuildWinBanner(RectTransform parent, WordPuzzleManager manager)
        {
            var panelGo = new GameObject("WinBanner", typeof(Image));
            panelGo.transform.SetParent(parent, false);
            var panelImg = panelGo.GetComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.55f);
            Stretch(panelImg.rectTransform);

            // Positioned below the chest; height leaves room for both buttons inside the rounded card.
            var card = CreateImage(panelImg.rectTransform, "Card", PuzzleArt.Tile(),
                new Color(0.16f, 0.12f, 0.24f, 0.98f));
            Center(card.rectTransform, new Vector2(860, 720), new Vector2(0, -120));

            var title = CreateText(card.rectTransform, "Title", "Скриня відкрита!", 58,
                TextAnchor.UpperCenter, new Color(1f, 0.85f, 0.4f));
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(780, 80), new Vector2(0, -45));

            var word = CreateText(card.rectTransform, "Word", "«ВОЛЯ» складено!", 40,
                TextAnchor.UpperCenter, Color.white);
            Anchor(word.rectTransform, new Vector2(0.5f, 1f), new Vector2(780, 60), new Vector2(0, -130));

            var reward = CreateText(card.rectTransform, "Reward",
                "Нагорода: Срібний Компас у AR", 34, TextAnchor.MiddleCenter,
                new Color(0.8f, 0.9f, 1f));
            Anchor(reward.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(780, 60), new Vector2(0, 70));

            var arBtn = CreateButton(card.rectTransform, "ARButton", "ВІДКРИТИ В AR",
                new Color(0.2f, 0.55f, 0.45f), () => manager.ShowAR());
            Center(arBtn, new Vector2(520, 120), new Vector2(0, -40));

            var replayBtn = CreateButton(card.rectTransform, "ReplayButton", "ГРАТИ ЗНОВУ",
                new Color(0.35f, 0.25f, 0.45f), () => manager.Restart());
            Center(replayBtn, new Vector2(520, 110), new Vector2(0, -185));

            panelGo.SetActive(false);
            return panelGo;
        }

        private GameObject BuildAROverlay(RectTransform parent, WordPuzzleManager manager,
            out RawImage arBackground, out RawImage compassImage)
        {
            var overlayGo = new GameObject("AROverlay", typeof(RectTransform));
            overlayGo.transform.SetParent(parent, false);
            var overlayRect = (RectTransform)overlayGo.transform;
            Stretch(overlayRect);

            arBackground = CreateRawImage(overlayRect, "ARBackground",
                new Color(0.06f, 0.09f, 0.14f, 1f));
            Stretch(arBackground.rectTransform);

            var header = CreateText(overlayRect, "ARHeader", "AR • Срібний Компас", 44,
                TextAnchor.UpperCenter, Color.white);
            Anchor(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(900, 70), new Vector2(0, -70));

            compassImage = CreateRawImage(overlayRect, "Compass", Color.white);
            Center(compassImage.rectTransform, new Vector2(620, 620), new Vector2(0, 40));

            var hint = CreateText(overlayRect, "ARHint",
                "Наведи камеру на поверхню — компас перед тобою", 30,
                TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.85f));
            Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(900, 80), new Vector2(0, 240));

            var back = CreateButton(overlayRect, "BackButton", "НАЗАД",
                new Color(0.3f, 0.3f, 0.35f), () => manager.HideAR());
            Center(back, new Vector2(360, 110), new Vector2(0, 90));

            overlayGo.SetActive(false);
            return overlayGo;
        }

        // ---------- UI helpers ----------

        private Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        private RawImage CreateRawImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RawImage));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<RawImage>();
            img.color = color;
            return img;
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

        private RectTransform CreateButton(Transform parent, string name, string label, Color color,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(onClick);

            var text = CreateText(go.transform, "Label", label, 36, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform);
            return (RectTransform)go.transform;
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
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.orthographic = true;
        }

        // ---------- RectTransform helpers ----------

        private static void Center(RectTransform rt, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = anchor;
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

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
