using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Object = UnityEngine.Object;

namespace Wonderland.UI.Editor
{
    public static class WonderlandUISetup
    {
        private const string MainScenePath = "Assets/_Project/World/Persistent/World_WonderlandPark.unity";
        private const string DataFolder = "Assets/_Project/UI/Data";
        private const string PrefabsFolder = "Assets/_Project/UI/Prefabs";
        private const string WelcomeFolder = "Assets/_Project/UI/WelcomeBoard";
        private const string NoticePanelPrefabPath = PrefabsFolder + "/WW_NoticeBoardOverlayPanel.prefab";
        private const string MenuPrefabPath = PrefabsFolder + "/WW_VRSystemMenu.prefab";
        private const string WelcomeContentPath = WelcomeFolder + "/WelcomeBoardNoticeContent.asset";

        [MenuItem("Wonderful World/UI/Build Notice Board UI Framework")]
        public static void BuildNoticeBoardUIFramework()
        {
            EnsureFolders();
            AssetDatabase.Refresh();

            LocalizedNoticeBoardContent welcomeContent = CreateWelcomeBoardContent();
            CreateNoticePanelPrefab();
            CreateSystemMenuPrefab();

            EditorUtility.SetDirty(welcomeContent);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Wonderland UI] Notice board UI framework generated.");
        }

        [MenuItem("Wonderful World/UI/Install Welcome Board Sample")]
        public static void InstallWelcomeBoardSample()
        {
            BuildNoticeBoardUIFramework();

            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            InstallIntoOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Wonderland UI] Welcome Board sample installed into " + MainScenePath);
        }

        public static void BuildAndInstallWelcomeBoard()
        {
            InstallWelcomeBoardSample();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project/UI");
            EnsureFolder(DataFolder);
            EnsureFolder(PrefabsFolder);
            EnsureFolder(WelcomeFolder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folder = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static LocalizedNoticeBoardContent CreateWelcomeBoardContent()
        {
            Sprite english = ImportSprite(WelcomeFolder + "/WelcomeBoard_en.png");
            Sprite chinese = ImportSprite(WelcomeFolder + "/WelcomeBoard_cn.png");
            Sprite swedish = ImportSprite(WelcomeFolder + "/WelcomeBoard_sw.png");

            LocalizedNoticeBoardContent content = AssetDatabase.LoadAssetAtPath<LocalizedNoticeBoardContent>(WelcomeContentPath);
            if (content == null)
            {
                content = ScriptableObject.CreateInstance<LocalizedNoticeBoardContent>();
                AssetDatabase.CreateAsset(content, WelcomeContentPath);
            }

            var sprites = new List<LocalizedSpriteSet>
            {
                new LocalizedSpriteSet { language = UILanguage.English, sprite = english },
                new LocalizedSpriteSet { language = UILanguage.ChineseSimplified, sprite = chinese },
                new LocalizedSpriteSet { language = UILanguage.Swedish, sprite = swedish }
            };

            content.SetEditorData("welcome-board", "Welcome Board", english, sprites);
            EditorUtility.SetDirty(content);
            return content;
        }

        private static Sprite ImportSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }

                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
            else
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject CreateNoticePanelPrefab()
        {
            GameObject root = CreateWorldCanvasRoot("WW_NoticeBoardOverlayPanel", new Vector2(1400f, 950f));
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            LocalizedNoticeBoardPanel panel = root.AddComponent<LocalizedNoticeBoardPanel>();

            Image posterImage = CreateImage("PosterContentImage", root.transform, new Vector2(1120f, 760f), new Vector2(0f, 48f), Color.white, 1);
            posterImage.preserveAspect = true;
            posterImage.raycastTarget = true;

            Button closeButton = CreateButton("CloseButton", root.transform, "Close", new Vector2(220f, 58f), new Vector2(0f, -390f), new Color(0.90f, 0.95f, 0.94f, 1f), new Color(0.08f, 0.12f, 0.14f, 1f), 2);
            AddLocalizedText(closeButton.GetComponentInChildren<TMP_Text>(true), "Close", "关闭", "Stäng");

            SetObject(panel, "canvasGroup", canvasGroup);
            SetObject(panel, "panelRect", root.GetComponent<RectTransform>());
            SetObject(panel, "contentImage", posterImage);
            SetObject(panel, "closeButton", closeButton);
            SetVector3(panel, "defaultWorldScale", new Vector3(0.00125f, 0.00125f, 0.00125f));

            GameObject prefab = SavePrefab(root, NoticePanelPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateSystemMenuPrefab()
        {
            GameObject root = CreateWorldCanvasRoot("WW_VRSystemMenu", new Vector2(760f, 520f));
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            VRSystemMenuController controller = root.AddComponent<VRSystemMenuController>();

            Image background = CreateImage("MenuBackground", root.transform, new Vector2(720f, 480f), Vector2.zero, new Color(0.075f, 0.09f, 0.1f, 0.96f), 0);
            background.raycastTarget = true;

            GameObject mainPanel = CreatePanel("MainPanel", root.transform, new Vector2(720f, 480f), Vector2.zero);
            TMP_Text mainTitle = CreateText("Title", mainPanel.transform, "Menu", 42f, FontStyles.Bold, new Vector2(0f, 170f), new Vector2(600f, 64f), Color.white);
            Button settingsButton = CreateButton("SettingsButton", mainPanel.transform, "Settings", new Vector2(360f, 68f), new Vector2(0f, 70f), new Color(0.90f, 0.95f, 0.94f, 1f), new Color(0.08f, 0.12f, 0.14f, 1f), 1);
            Button cancelButton = CreateButton("CancelButton", mainPanel.transform, "Cancel", new Vector2(360f, 68f), new Vector2(0f, -20f), new Color(0.90f, 0.95f, 0.94f, 1f), new Color(0.08f, 0.12f, 0.14f, 1f), 1);
            Button exitButton = CreateButton("ExitButton", mainPanel.transform, "Exit", new Vector2(360f, 68f), new Vector2(0f, -110f), new Color(0.68f, 0.18f, 0.16f, 1f), Color.white, 1);

            GameObject settingsPanel = CreatePanel("SettingsPanel", root.transform, new Vector2(720f, 480f), Vector2.zero);
            TMP_Text settingsTitle = CreateText("Title", settingsPanel.transform, "Settings", 40f, FontStyles.Bold, new Vector2(0f, 176f), new Vector2(600f, 58f), Color.white);
            TMP_Text languageLabel = CreateText("LanguageLabel", settingsPanel.transform, "Language", 24f, FontStyles.Normal, new Vector2(-205f, 104f), new Vector2(200f, 42f), new Color(0.82f, 0.88f, 0.86f, 1f));

            Button englishButton = CreateButton("EnglishButton", settingsPanel.transform, "English", new Vector2(440f, 58f), new Vector2(80f, 78f), new Color(0.90f, 0.95f, 0.94f, 1f), new Color(0.08f, 0.12f, 0.14f, 1f), 1);
            Button chineseButton = CreateButton("ChineseButton", settingsPanel.transform, "Chinese", new Vector2(440f, 58f), new Vector2(80f, 8f), new Color(0.90f, 0.95f, 0.94f, 1f), new Color(0.08f, 0.12f, 0.14f, 1f), 1);
            Button swedishButton = CreateButton("SwedishButton", settingsPanel.transform, "Swedish", new Vector2(440f, 58f), new Vector2(80f, -62f), new Color(0.90f, 0.95f, 0.94f, 1f), new Color(0.08f, 0.12f, 0.14f, 1f), 1);
            Button backButton = CreateButton("BackButton", settingsPanel.transform, "Back", new Vector2(260f, 58f), new Vector2(0f, -160f), new Color(0.90f, 0.95f, 0.94f, 1f), new Color(0.08f, 0.12f, 0.14f, 1f), 1);

            AddLocalizedText(mainTitle, "Menu", "菜单", "Meny");
            AddLocalizedText(settingsButton.GetComponentInChildren<TMP_Text>(true), "Settings", "设置", "Inställningar");
            AddLocalizedText(cancelButton.GetComponentInChildren<TMP_Text>(true), "Cancel", "取消", "Avbryt");
            AddLocalizedText(exitButton.GetComponentInChildren<TMP_Text>(true), "Exit", "退出", "Avsluta");
            AddLocalizedText(settingsTitle, "Settings", "设置", "Inställningar");
            AddLocalizedText(languageLabel, "Language", "语言", "Språk");
            AddLocalizedText(englishButton.GetComponentInChildren<TMP_Text>(true), "English", "英语", "Engelska");
            AddLocalizedText(chineseButton.GetComponentInChildren<TMP_Text>(true), "Chinese", "中文", "Kinesiska");
            AddLocalizedText(swedishButton.GetComponentInChildren<TMP_Text>(true), "Swedish", "瑞典语", "Svenska");
            AddLocalizedText(backButton.GetComponentInChildren<TMP_Text>(true), "Back", "返回", "Tillbaka");

            VRSettingsMenuView settingsView = settingsPanel.AddComponent<VRSettingsMenuView>();
            SetObject(settingsView, "englishButton", englishButton);
            SetObject(settingsView, "chineseButton", chineseButton);
            SetObject(settingsView, "swedishButton", swedishButton);
            SetObject(settingsView, "englishLabel", englishButton.GetComponentInChildren<TMP_Text>(true));
            SetObject(settingsView, "chineseLabel", chineseButton.GetComponentInChildren<TMP_Text>(true));
            SetObject(settingsView, "swedishLabel", swedishButton.GetComponentInChildren<TMP_Text>(true));

            SetObject(controller, "canvasGroup", canvasGroup);
            SetObject(controller, "mainPanel", mainPanel);
            SetObject(controller, "settingsPanel", settingsPanel);
            SetObject(controller, "settingsButton", settingsButton);
            SetObject(controller, "cancelButton", cancelButton);
            SetObject(controller, "exitButton", exitButton);
            SetObject(controller, "backButton", backButton);
            SetVector3(controller, "worldScale", new Vector3(0.0015f, 0.0015f, 0.0015f));

            VRSystemMenuHierarchyBaker.Bake(root);
            settingsPanel.SetActive(false);

            GameObject prefab = SavePrefab(root, MenuPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateWorldCanvasRoot(string name, Vector2 size)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
            RectTransform rectTransform = root.GetComponent<RectTransform>();
            rectTransform.sizeDelta = size;
            rectTransform.localScale = Vector3.one;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = null;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 1f;

            return root;
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 size, Vector2 position)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return panel;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 position, Color color, int siblingIndex)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            imageObject.transform.SetSiblingIndex(siblingIndex);

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, float fontSize, FontStyles style, Vector2 position, Vector2 size, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 size, Vector2 position, Color backgroundColor, Color textColor, int siblingIndex)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            buttonObject.transform.SetSiblingIndex(siblingIndex);

            Image image = buttonObject.GetComponent<Image>();
            image.color = backgroundColor;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.78f, 0.94f, 1f, 1f);
            colors.pressedColor = new Color(0.68f, 0.78f, 0.82f, 1f);
            colors.selectedColor = new Color(0.78f, 0.94f, 1f, 1f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            if (!string.IsNullOrEmpty(label))
            {
                TMP_Text text = CreateText("Label", buttonObject.transform, label, 25f, FontStyles.Bold, Vector2.zero, size, textColor);
                text.enableAutoSizing = true;
                text.fontSizeMin = 16f;
                text.fontSizeMax = 25f;
            }

            return button;
        }

        private static void AddLocalizedText(TMP_Text target, string english, string chineseSimplified, string swedish)
        {
            if (target == null)
            {
                return;
            }

            LocalizedUIText localizedText = target.GetComponent<LocalizedUIText>();
            if (localizedText == null)
            {
                localizedText = target.gameObject.AddComponent<LocalizedUIText>();
            }

            localizedText.SetTexts(english, chineseSimplified, swedish);
            EditorUtility.SetDirty(localizedText);
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }

        private static void InstallIntoOpenScene()
        {
            GameObject systemRoot = FindSceneObject("WW_UI_System");
            if (systemRoot == null)
            {
                systemRoot = new GameObject("WW_UI_System");
            }

            if (systemRoot.GetComponent<UILanguageService>() == null)
            {
                systemRoot.AddComponent<UILanguageService>();
            }

            LocalizedNoticeBoardPanel noticePanel = EnsurePrefabInstance<LocalizedNoticeBoardPanel>(NoticePanelPrefabPath, "WW_NoticeBoardOverlayPanel", systemRoot.transform);
            VRSystemMenuController systemMenu = EnsureSystemMenuInstance(systemRoot.transform);
            VRSystemMenuHierarchyBaker.Bake(systemMenu.gameObject);

            GameObject board = FindSceneObject("Notice_Board");
            if (board == null)
            {
                board = FindSceneObject("Welcome Board");
            }

            if (board == null)
            {
                board = FindSceneObject("medieval_notice_board");
            }

            if (board == null)
            {
                Debug.LogWarning("[Wonderland UI] Could not find Notice_Board, Welcome Board, or medieval_notice_board in the open scene.");
                return;
            }

            Transform panelSurfaceTransform = FindNoticeBoardPanelSurface(board.transform);
            Bounds panelBounds = panelSurfaceTransform != null
                ? CalculateRendererBounds(panelSurfaceTransform.gameObject)
                : CalculateRendererBounds(board);
            EnsureClickableCollider(board, panelBounds);

            XRSimpleInteractable interactable = board.GetComponent<XRSimpleInteractable>();
            if (interactable == null)
            {
                interactable = board.AddComponent<XRSimpleInteractable>();
            }

            NoticeBoardHotspot hotspot = board.GetComponent<NoticeBoardHotspot>();
            if (hotspot == null)
            {
                hotspot = board.AddComponent<NoticeBoardHotspot>();
            }

            Transform anchor = EnsureWelcomeBoardAnchor(board.transform, panelBounds);
            LocalizedNoticeBoardContent content = AssetDatabase.LoadAssetAtPath<LocalizedNoticeBoardContent>(WelcomeContentPath);

            SetObject(hotspot, "content", content);
            SetObject(hotspot, "panel", noticePanel);
            SetObject(hotspot, "panelAnchor", anchor);
            SetObject(hotspot, "interactable", interactable);
            SetBool(hotspot, "useBoardAnchorForPopup", false);
            SetBool(hotspot, "openWithRightIndexTrigger", true);
            SetVector3(hotspot, "panelWorldScale", new Vector3(0.00125f, 0.00125f, 0.00125f));

            LocalizedNoticeBoardSurface surface = EnsureBoardSurface(board, content, panelSurfaceTransform, panelBounds);

            EditorUtility.SetDirty(systemRoot);
            EditorUtility.SetDirty(board);
            EditorUtility.SetDirty(hotspot);
            if (surface != null)
            {
                EditorUtility.SetDirty(surface);
            }

            EditorUtility.SetDirty(systemMenu);
            EditorUtility.SetDirty(noticePanel);
        }

        private static VRSystemMenuController EnsureSystemMenuInstance(Transform systemRoot)
        {
            if (systemRoot != null)
            {
                VRSystemMenuController existingUnderSystemRoot = systemRoot.GetComponentInChildren<VRSystemMenuController>(true);
                if (existingUnderSystemRoot != null)
                {
                    return existingUnderSystemRoot;
                }
            }

            return EnsurePrefabInstance<VRSystemMenuController>(MenuPrefabPath, "WW_VRSystemMenu", systemRoot);
        }

        private static LocalizedNoticeBoardSurface EnsureBoardSurface(GameObject board, LocalizedNoticeBoardContent content, Transform panelSurfaceTransform, Bounds panelBounds)
        {
            Transform baseSurface = panelSurfaceTransform != null ? panelSurfaceTransform : board.transform;
            Transform surfaceTransform = baseSurface.Find("NoticeBoardSurfacePoster");
            if (surfaceTransform == null)
            {
                surfaceTransform = new GameObject("NoticeBoardSurfacePoster").transform;
                surfaceTransform.SetParent(baseSurface, false);
            }

            SpriteRenderer spriteRenderer = surfaceTransform.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = surfaceTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sortingOrder = 50;

            Vector3 boardForward = EstimateForwardFromViewer(panelBounds);
            surfaceTransform.position = panelBounds.center - boardForward * 0.015f;
            surfaceTransform.rotation = Quaternion.LookRotation(boardForward, Vector3.up);

            Sprite englishSprite = content != null ? content.GetSprite(UILanguage.English) : null;
            if (englishSprite != null)
            {
                float spriteWorldWidth = englishSprite.bounds.size.x;
                float spriteWorldHeight = englishSprite.bounds.size.y;
                float targetWidth = Mathf.Max(0.05f, panelBounds.size.x * 0.82f);
                float targetHeight = Mathf.Max(0.05f, panelBounds.size.y * 0.82f);
                float scale = Mathf.Min(targetWidth / spriteWorldWidth, targetHeight / spriteWorldHeight);
                surfaceTransform.localScale = new Vector3(scale, scale, scale);
            }

            LocalizedNoticeBoardSurface surface = surfaceTransform.GetComponent<LocalizedNoticeBoardSurface>();
            if (surface == null)
            {
                surface = surfaceTransform.gameObject.AddComponent<LocalizedNoticeBoardSurface>();
            }

            SetObject(surface, "content", content);
            SetObject(surface, "targetSpriteRenderer", spriteRenderer);
            SetBool(surface, "useMaterialPropertyBlock", true);
            return surface;
        }

        private static Transform FindNoticeBoardPanelSurface(Transform board)
        {
            Transform surface = FindChildRecursive(board, "Poster_back");
            if (surface != null)
            {
                return surface;
            }

            surface = FindChildRecursive(board, "Pages");
            if (surface != null)
            {
                return surface;
            }

            surface = FindChildRecursive(board, "Mainbody");
            if (surface != null)
            {
                return surface;
            }

            return null;
        }

        private static T EnsurePrefabInstance<T>(string prefabPath, string instanceName, Transform parent) where T : Component
        {
            T existing = FindSceneComponent<T>(instanceName);
            if (existing != null)
            {
                return existing;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject instance = prefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
                : new GameObject(instanceName);

            instance.name = instanceName;
            instance.transform.SetParent(parent, false);

            T component = instance.GetComponent<T>();
            if (component == null)
            {
                component = instance.AddComponent<T>();
            }

            return component;
        }

        private static Transform EnsureWelcomeBoardAnchor(Transform board, Bounds bounds)
        {
            Transform anchor = board.Find("WelcomeBoardPanelAnchor");
            if (anchor == null)
            {
                anchor = new GameObject("WelcomeBoardPanelAnchor").transform;
                anchor.SetParent(board, true);
            }

            Vector3 viewerPosition = GetViewerReferencePosition();
            Vector3 toBoard = bounds.center - viewerPosition;
            Vector3 forward = Vector3.ProjectOnPlane(toBoard, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.ProjectOnPlane(board.forward, Vector3.up).normalized;
            }

            Vector3 anchorPosition = bounds.center - forward * 0.08f;
            anchorPosition.y = bounds.center.y + Mathf.Min(bounds.extents.y * 0.12f, 0.35f);
            Quaternion anchorRotation = Quaternion.LookRotation(forward, Vector3.up);

            anchor.SetPositionAndRotation(anchorPosition, anchorRotation);
            return anchor;
        }

        private static Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrEmpty(targetName))
            {
                return null;
            }

            if (root.name == targetName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), targetName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Vector3 GetViewerReferencePosition()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                return camera.transform.position;
            }

            GameObject xrRoot = FindSceneObject("XR");
            if (xrRoot != null)
            {
                return xrRoot.transform.position;
            }

            return Vector3.zero;
        }

        private static Vector3 EstimateForwardFromViewer(Bounds bounds)
        {
            Vector3 toBoard = bounds.center - GetViewerReferencePosition();
            Vector3 forward = Vector3.ProjectOnPlane(toBoard, Vector3.up).normalized;
            return forward.sqrMagnitude > 0.001f ? forward : Vector3.forward;
        }

        private static void EnsureClickableCollider(GameObject target, Bounds worldBounds)
        {
            BoxCollider boxCollider = target.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = target.AddComponent<BoxCollider>();
            }

            Bounds localBounds = WorldBoundsToLocal(target.transform, worldBounds);
            Vector3 size = localBounds.size;
            size.x = Mathf.Max(size.x, 0.2f);
            size.y = Mathf.Max(size.y, 0.2f);
            size.z = Mathf.Max(size.z, 0.2f);

            boxCollider.center = localBounds.center;
            boxCollider.size = size;
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position + Vector3.up, new Vector3(1.6f, 1.6f, 0.2f));
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static Bounds WorldBoundsToLocal(Transform root, Bounds worldBounds)
        {
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldCorner = worldBounds.center + Vector3.Scale(worldBounds.extents, new Vector3(x, y, z));
                        Vector3 localCorner = root.InverseTransformPoint(worldCorner);
                        min = Vector3.Min(min, localCorner);
                        max = Vector3.Max(max, localCorner);
                    }
                }
            }

            Bounds localBounds = new Bounds();
            localBounds.SetMinMax(min, max);
            return localBounds;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (transform == null || transform.name != objectName)
                {
                    continue;
                }

                GameObject gameObject = transform.gameObject;
                if (gameObject.scene.IsValid() && !EditorUtility.IsPersistent(gameObject))
                {
                    return gameObject;
                }
            }

            return null;
        }

        private static T FindSceneComponent<T>(string objectName) where T : Component
        {
            GameObject gameObject = FindSceneObject(objectName);
            return gameObject != null ? gameObject.GetComponent<T>() : null;
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetVector3(Object target, string propertyName, Vector3 value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.vector3Value = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
