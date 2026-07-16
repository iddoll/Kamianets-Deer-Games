#if UNITY_EDITOR
using LivingStone.StonePuzzle;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LivingStone.StonePuzzle.Editor
{
    /// <summary>
    /// Landscape layout: wall sprite + hotspots, side panel slides in from the right.
    /// Menu: Living Stone → Setup Puzzle Scene
    /// </summary>
    public static class StonePuzzleSetup
    {
        // Keep hotspots on the LEFT ~60% so the side panel never covers them when closed,
        // and the correct stone stays reachable before the panel opens.
        static readonly Vector4[] HotspotLayout =
        {
            new Vector4(0.16f, 0.42f, 0.10f, 0.16f),
            new Vector4(0.30f, 0.55f, 0.10f, 0.16f),
            new Vector4(0.42f, 0.38f, 0.10f, 0.16f),
            new Vector4(0.50f, 0.52f, 0.10f, 0.16f),
            new Vector4(0.58f, 0.44f, 0.11f, 0.18f), // smooth stone
        };

        const int SmoothStoneIndex = 4;

        [MenuItem("Living Stone/Setup Puzzle Scene")]
        public static void SetupPuzzleScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("Open SampleScene first.");
                return;
            }

            ApplyLandscapePlayerSettings();
            EnsureEventSystem();
            var canvas = EnsureCanvas();
            var manager = EnsureManager();

            ClearPreviousScaffold(canvas.transform);

            // --- Left / full: wall ---
            var gameView = CreateUiObject("GameView", canvas.transform);
            StretchFull(gameView);

            var wall = CreateUiObject("WallImage", gameView);
            StretchFull(wall);
            var wallImage = wall.gameObject.AddComponent<Image>();
            wallImage.color = new Color(0.55f, 0.58f, 0.52f, 1f);
            wallImage.raycastTarget = false;

            var wallPlaceholder = CreateTmpLabel(wall, "WallPlaceholderLabel",
                "Сюди — спрайт стіни фортеці\n(камені = hotspot поверх картинки)", 32);
            wallPlaceholder.rectTransform.anchorMin = new Vector2(0.05f, 0.35f);
            wallPlaceholder.rectTransform.anchorMax = new Vector2(0.55f, 0.65f);
            wallPlaceholder.rectTransform.offsetMin = Vector2.zero;
            wallPlaceholder.rectTransform.offsetMax = Vector2.zero;
            wallPlaceholder.alignment = TextAlignmentOptions.Center;
            wallPlaceholder.color = new Color(0.2f, 0.22f, 0.2f, 0.75f);

            var bannerLeft = CreateRibbon(gameView, "LeftBanner", "ПЛАН ФОРТЕЦІ (КОВПАК)",
                new Vector2(0.02f, 0.88f), new Vector2(0.48f, 0.98f),
                new Color(0.12f, 0.35f, 0.38f, 0.95f));

            var hotspotsRoot = CreateUiObject("Hotspots", wall);
            StretchFull(hotspotsRoot);

            ClickableStone smooth = null;
            var stoneRefs = new ClickableStone[HotspotLayout.Length];
            CanvasGroup hatGroup = null;

            for (int i = 0; i < HotspotLayout.Length; i++)
            {
                bool isSmooth = i == SmoothStoneIndex;
                CreateHotspot(hotspotsRoot, i, HotspotLayout[i], isSmooth, out var clickable, out var hat);
                stoneRefs[i] = clickable;
                if (isSmooth)
                {
                    smooth = clickable;
                    hatGroup = hat;
                }
            }

            // --- Right side panel (~38% width, fully off-screen until stone found) ---
            var sidePanel = CreateUiObject("SidePanel", canvas.transform);
            sidePanel.anchorMin = new Vector2(0.62f, 0f);
            sidePanel.anchorMax = new Vector2(1f, 1f);
            sidePanel.pivot = new Vector2(0f, 0.5f);
            sidePanel.offsetMin = Vector2.zero;
            sidePanel.offsetMax = Vector2.zero;
            sidePanel.anchoredPosition = new Vector2(1200f, 0f); // fully off to the right

            var panelBg = sidePanel.gameObject.AddComponent<Image>();
            panelBg.color = new Color(0.90f, 0.86f, 0.76f, 0.97f);

            var sideGroup = sidePanel.gameObject.AddComponent<CanvasGroup>();
            sideGroup.alpha = 0f;
            sideGroup.blocksRaycasts = false;
            sideGroup.interactable = false;

            CreateRibbon(sidePanel, "RightBanner", "ЗАВДАННЯ 1",
                new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.97f),
                new Color(0.12f, 0.35f, 0.38f, 0.95f));

            var title = CreateTmpLabel(sidePanel, "Title", "ЖИВИЙ КАМІНЬ", 48);
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.78f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.87f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.18f, 0.16f, 0.12f, 1f);

            // Dragon + hint row
            var dragon = CreateUiObject("DragonPortrait", sidePanel);
            dragon.anchorMin = new Vector2(0.08f, 0.42f);
            dragon.anchorMax = new Vector2(0.38f, 0.74f);
            dragon.offsetMin = Vector2.zero;
            dragon.offsetMax = Vector2.zero;
            var dragonImage = dragon.gameObject.AddComponent<Image>();
            dragonImage.color = new Color(0.25f, 0.45f, 0.35f, 1f);

            var dragonLabel = CreateTmpLabel(dragon, "DragonPlaceholder", "DRAGON\nIMAGE", 22);
            StretchFull(dragonLabel.rectTransform);
            dragonLabel.alignment = TextAlignmentOptions.Center;
            dragonLabel.color = new Color(0.9f, 0.95f, 0.9f, 0.9f);

            var hintBubble = CreateUiObject("HintBubble", sidePanel);
            hintBubble.anchorMin = new Vector2(0.40f, 0.42f);
            hintBubble.anchorMax = new Vector2(0.94f, 0.74f);
            hintBubble.offsetMin = Vector2.zero;
            hintBubble.offsetMax = Vector2.zero;
            var bubbleBg = hintBubble.gameObject.AddComponent<Image>();
            bubbleBg.color = new Color(1f, 0.98f, 0.93f, 1f);

            var hint = CreateTmpLabel(hintBubble, "HintLabel",
                "Мій ніс відчуває тепло. Шукай камінь, який за текстурою гладкий і річковий, а не шерехатий вапняк!",
                22);
            StretchFull(hint.rectTransform);
            hint.rectTransform.offsetMin = new Vector2(16f, 12f);
            hint.rectTransform.offsetMax = new Vector2(-16f, -12f);
            hint.alignment = TextAlignmentOptions.TopLeft;
            hint.enableWordWrapping = true;
            hint.color = new Color(0.2f, 0.18f, 0.14f, 1f);

            // Answer row: input + door button
            var inputGo = CreateUiObject("PasswordInput", sidePanel);
            inputGo.anchorMin = new Vector2(0.08f, 0.18f);
            inputGo.anchorMax = new Vector2(0.62f, 0.34f);
            inputGo.offsetMin = Vector2.zero;
            inputGo.offsetMax = Vector2.zero;

            var inputBg = inputGo.gameObject.AddComponent<Image>();
            inputBg.color = new Color(1f, 0.99f, 0.96f, 1f);

            var tmpInput = inputGo.gameObject.AddComponent<TMP_InputField>();
            var textArea = CreateUiObject("Text Area", inputGo);
            StretchFull(textArea);
            textArea.offsetMin = new Vector2(14f, 8f);
            textArea.offsetMax = new Vector2(-14f, -8f);

            var placeholder = CreateTmpLabel(textArea, "Placeholder", "Введи слово…", 28);
            StretchFull(placeholder.rectTransform);
            placeholder.color = new Color(0.45f, 0.42f, 0.38f, 0.7f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;

            var inputText = CreateTmpLabel(textArea, "Text", "", 28);
            StretchFull(inputText.rectTransform);
            inputText.color = Color.black;
            inputText.alignment = TextAlignmentOptions.MidlineLeft;

            tmpInput.textViewport = textArea;
            tmpInput.textComponent = inputText;
            tmpInput.placeholder = placeholder;
            tmpInput.fontAsset = inputText.font;

            var doorGo = CreateUiObject("DoorOpenButton", sidePanel);
            doorGo.anchorMin = new Vector2(0.68f, 0.12f);
            doorGo.anchorMax = new Vector2(0.94f, 0.38f);
            doorGo.offsetMin = Vector2.zero;
            doorGo.offsetMax = Vector2.zero;

            var doorImage = doorGo.gameObject.AddComponent<Image>();
            doorImage.color = new Color(0.45f, 0.32f, 0.18f, 1f);
            var submitButton = doorGo.gameObject.AddComponent<Button>();
            submitButton.targetGraphic = doorImage;

            var doorLabel = CreateTmpLabel(doorGo, "DoorLabel", "ДВЕРІ\n→", 30);
            StretchFull(doorLabel.rectTransform);
            doorLabel.alignment = TextAlignmentOptions.Center;
            doorLabel.color = Color.white;

            var footHint = CreateTmpLabel(sidePanel, "FootHint",
                "Тапни гладкий камінь на стіні → з’явиться шапка → введи «Ковпак»", 18);
            footHint.rectTransform.anchorMin = new Vector2(0.08f, 0.04f);
            footHint.rectTransform.anchorMax = new Vector2(0.92f, 0.12f);
            footHint.rectTransform.offsetMin = Vector2.zero;
            footHint.rectTransform.offsetMax = Vector2.zero;
            footHint.alignment = TextAlignmentOptions.Center;
            footHint.color = new Color(0.3f, 0.28f, 0.22f, 0.8f);

            // Full-screen win photo / card
            var winOverlayRt = CreateUiObject("WinOverlay", canvas.transform);
            StretchFull(winOverlayRt);
            winOverlayRt.SetAsLastSibling();

            var winDim = winOverlayRt.gameObject.AddComponent<Image>();
            winDim.color = new Color(0.05f, 0.05f, 0.05f, 0.55f);
            winDim.raycastTarget = true;

            var winCard = CreateUiObject("WinPhoto", winOverlayRt);
            winCard.anchorMin = new Vector2(0.18f, 0.12f);
            winCard.anchorMax = new Vector2(0.82f, 0.88f);
            winCard.offsetMin = Vector2.zero;
            winCard.offsetMax = Vector2.zero;

            var winPhoto = winCard.gameObject.AddComponent<Image>();
            winPhoto.color = new Color(0.18f, 0.42f, 0.28f, 1f);
            winPhoto.raycastTarget = false;

            var winTitle = CreateTmpLabel(winCard, "WinTitle", "ВИ ПЕРЕМОГЛИ", 72);
            winTitle.rectTransform.anchorMin = new Vector2(0.08f, 0.55f);
            winTitle.rectTransform.anchorMax = new Vector2(0.92f, 0.85f);
            winTitle.rectTransform.offsetMin = Vector2.zero;
            winTitle.rectTransform.offsetMax = Vector2.zero;
            winTitle.alignment = TextAlignmentOptions.Center;
            winTitle.fontStyle = FontStyles.Bold;
            winTitle.color = Color.white;

            var winSub = CreateTmpLabel(winCard, "WinSubtitle",
                "Двері вежі відчинено!\n(замініть цей блок на фото перемоги)", 28);
            winSub.rectTransform.anchorMin = new Vector2(0.1f, 0.2f);
            winSub.rectTransform.anchorMax = new Vector2(0.9f, 0.5f);
            winSub.rectTransform.offsetMin = Vector2.zero;
            winSub.rectTransform.offsetMax = Vector2.zero;
            winSub.alignment = TextAlignmentOptions.Center;
            winSub.color = new Color(0.95f, 0.95f, 0.9f, 0.95f);

            var winGroup = winOverlayRt.gameObject.AddComponent<CanvasGroup>();
            winGroup.alpha = 0f;
            winGroup.blocksRaycasts = false;
            winGroup.interactable = false;
            winOverlayRt.gameObject.SetActive(false);

            // Wire manager
            var audio = manager.GetComponent<AudioSource>();
            var so = new SerializedObject(manager);
            so.FindProperty("smoothStone").objectReferenceValue = smooth;
            so.FindProperty("hatOverlay").objectReferenceValue = hatGroup;
            so.FindProperty("sidePanel").objectReferenceValue = sidePanel;
            so.FindProperty("sidePanelGroup").objectReferenceValue = sideGroup;
            so.FindProperty("passwordInput").objectReferenceValue = tmpInput;
            so.FindProperty("submitButton").objectReferenceValue = submitButton;
            so.FindProperty("inputShakeTarget").objectReferenceValue = inputGo;
            so.FindProperty("dragonPortrait").objectReferenceValue = dragonImage;
            so.FindProperty("hintLabel").objectReferenceValue = hint;
            so.FindProperty("doorSuccessIcon").objectReferenceValue = doorImage;
            so.FindProperty("winOverlay").objectReferenceValue = winGroup;
            so.FindProperty("sfxSource").objectReferenceValue = audio;
            so.ApplyModifiedPropertiesWithoutUndo();

            for (int i = 0; i < stoneRefs.Length; i++)
            {
                var stoneSo = new SerializedObject(stoneRefs[i]);
                stoneSo.FindProperty("isSmoothStone").boolValue = i == SmoothStoneIndex;
                stoneSo.FindProperty("button").objectReferenceValue = stoneRefs[i].GetComponent<Button>();
                stoneSo.FindProperty("hotspotGraphic").objectReferenceValue = stoneRefs[i].GetComponent<Image>();
                stoneSo.FindProperty("showDebugOutline").boolValue = true;
                stoneSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(stoneRefs[i]);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = manager.gameObject;
            Debug.Log(
                "Landscape Living Stone ready.\n" +
                "1) Drop wall sprite on WallImage.\n" +
                "2) Move Hotspots onto the stones (gold ring = correct).\n" +
                "3) Drop dragon face on DragonPortrait, door art on DoorOpenButton.\n" +
                "4) Uncheck Show Debug Outline on hotspots when done.\n" +
                "Password: Ковпак");
        }

        [MenuItem("Living Stone/Fix: Hide Side Panel Off-Screen")]
        public static void FixHideSidePanel()
        {
            var panelGo = GameObject.Find("SidePanel");
            if (panelGo == null)
            {
                Debug.LogError("SidePanel not found. Run Setup Puzzle Scene first.");
                return;
            }

            var sidePanel = panelGo.GetComponent<RectTransform>();
            // Narrower right strip so wall stays free until panel opens.
            sidePanel.anchorMin = new Vector2(0.62f, 0f);
            sidePanel.anchorMax = new Vector2(1f, 1f);
            sidePanel.pivot = new Vector2(0f, 0.5f);
            sidePanel.offsetMin = Vector2.zero;
            sidePanel.offsetMax = Vector2.zero;
            sidePanel.anchoredPosition = new Vector2(1200f, 0f);

            var group = panelGo.GetComponent<CanvasGroup>();
            if (group == null)
                group = panelGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var manager = Object.FindFirstObjectByType<StonePuzzleManager>();
            if (manager != null)
            {
                var so = new SerializedObject(manager);
                so.FindProperty("sidePanel").objectReferenceValue = sidePanel;
                so.FindProperty("sidePanelGroup").objectReferenceValue = group;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(manager);
            }

            EditorUtility.SetDirty(panelGo);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("SidePanel parked off-screen and ignores clicks until the smooth stone is found.");
        }

        [MenuItem("Living Stone/Hide Hotspot Outlines")]
        public static void HideHotspotOutlines()
        {
            foreach (var stone in Object.FindObjectsByType<ClickableStone>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(stone);
                so.FindProperty("showDebugOutline").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                stone.SetDebugOutline(false);
                EditorUtility.SetDirty(stone);
            }

            Debug.Log("Hotspot outlines hidden (still clickable).");
        }

        static void ApplyLandscapePlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }

        static void ClearPreviousScaffold(Transform canvas)
        {
            string[] names =
            {
                "GameView", "SidePanel", "WinOverlay", "StoneWall", "HatOverlay", "PasswordPanel", "HintText",
                "WallImage"
            };
            foreach (var n in names)
            {
                var existing = canvas.Find(n);
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);
            }
        }

        static StonePuzzleManager EnsureManager()
        {
            var existing = Object.FindFirstObjectByType<StonePuzzleManager>();
            if (existing != null)
                return existing;

            var go = new GameObject("StonePuzzleManager", typeof(AudioSource));
            return go.AddComponent<StonePuzzleManager>();
        }

        static Canvas EnsureCanvas()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject go;
            if (canvas == null)
            {
                go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = go.GetComponent<Canvas>();
            }
            else
            {
                go = canvas.gameObject;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = go.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        static void EnsureEventSystem()
        {
            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem));
                go.AddComponent<InputSystemUIInputModule>();
                return;
            }

            if (es.GetComponent<InputSystemUIInputModule>() == null &&
                es.GetComponent<StandaloneInputModule>() == null)
            {
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        static void CreateHotspot(
            Transform parent,
            int index,
            Vector4 layout,
            bool isSmooth,
            out ClickableStone clickable,
            out CanvasGroup hatGroup)
        {
            hatGroup = null;
            float cx = layout.x;
            float cy = layout.y;
            float w = layout.z;
            float h = layout.w;

            var name = isSmooth ? "SmoothStoneHotspot" : $"StoneHotspot_{index}";
            var rt = CreateUiObject(name, parent);
            rt.anchorMin = new Vector2(cx - w * 0.5f, cy - h * 0.5f);
            rt.anchorMax = new Vector2(cx + w * 0.5f, cy + h * 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.2f);

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.35f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.5f);
            button.colors = colors;

            clickable = rt.gameObject.AddComponent<ClickableStone>();

            if (!isSmooth)
                return;

            var hat = CreateUiObject("HatOverlay", rt);
            StretchFull(hat);
            hat.anchorMin = new Vector2(-0.15f, -0.1f);
            hat.anchorMax = new Vector2(1.15f, 1.25f);

            var hatImage = hat.gameObject.AddComponent<Image>();
            hatImage.color = new Color(0.75f, 0.15f, 0.12f, 0.95f);
            hatImage.raycastTarget = false;

            var hatLabel = CreateTmpLabel(hat, "HatPlaceholder", "ШАПКА\n(AR)", 20);
            StretchFull(hatLabel.rectTransform);
            hatLabel.alignment = TextAlignmentOptions.Center;
            hatLabel.color = Color.white;

            hatGroup = hat.gameObject.AddComponent<CanvasGroup>();
            hatGroup.alpha = 0f;
            hatGroup.blocksRaycasts = false;
            hatGroup.interactable = false;
        }

        static RectTransform CreateRibbon(
            Transform parent,
            string name,
            string text,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            var rt = CreateUiObject(name, parent);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            var label = CreateTmpLabel(rt, "Label", text, 26);
            StretchFull(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            return rt;
        }

        static RectTransform CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static TextMeshProUGUI CreateTmpLabel(Transform parent, string name, string text, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
#endif
