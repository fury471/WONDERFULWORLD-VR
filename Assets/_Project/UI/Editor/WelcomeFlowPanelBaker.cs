using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Wonderland.UI.Editor
{
    public static class WelcomeFlowPanelBaker
    {
        private const string MainScenePath = "Assets/_Project/World/Persistent/World_WonderlandPark.unity";
        private const string CjkFontPath = "Assets/_Project/UI/Resources/Fonts/NotoSansCJKsc-Regular.otf";

        private static readonly Color BackgroundColor = new Color(0.075f, 0.09f, 0.10f, 0.96f);
        private static readonly Color PanelBandColor = new Color(0.105f, 0.13f, 0.145f, 0.78f);
        private static readonly Color AccentColor = new Color(0.18f, 0.58f, 0.72f, 1f);
        private static readonly Color PrimaryButtonColor = new Color(0.18f, 0.58f, 0.72f, 1f);
        private static readonly Color NormalButtonColor = new Color(0.90f, 0.95f, 0.94f, 1f);
        private static readonly Color BodyTextColor = new Color(0.86f, 0.91f, 0.90f, 1f);
        private static readonly Color DarkTextColor = new Color(0.08f, 0.12f, 0.14f, 1f);

        private struct LabelSpec
        {
            public TMP_Text target;
            public string english;
            public string chinese;
            public string swedish;
        }

        [MenuItem("Wonderful World/UI/Rebuild Welcome Flow Panel")]
        public static void RebuildMainScenePanel()
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            RebuildOpenScenePanel();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void RebuildOpenScenePanel()
        {
            EnsureProjectCjkFontImported();
            DestroyExistingWelcomePanels();

            GameObject systemRoot = EnsureUiSystemRoot();
            GameObject root = new GameObject(
                "WelcomePanel",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(TrackedDeviceGraphicRaycaster),
                typeof(CanvasGroup),
                typeof(WelcomeFlowController));
            root.transform.SetParent(systemRoot.transform, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(920f, 620f);
            rootRect.localScale = Vector3.one;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = null;
            canvas.sortingOrder = 120;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 1f;

            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            List<LabelSpec> labels = new List<LabelSpec>();
            BuildHierarchy(rootRect, labels, out Button startButton, out Button englishButton, out Button chineseButton, out Button swedishButton);
            ConfigureController(root.GetComponent<WelcomeFlowController>(), canvasGroup, startButton, englishButton, chineseButton, swedishButton, labels);

            EditorUtility.SetDirty(systemRoot);
            EditorUtility.SetDirty(root);
            Debug.Log("[WelcomeFlowPanelBaker] Rebuilt WelcomePanel in the open scene.");
        }

        private static void BuildHierarchy(RectTransform root, List<LabelSpec> labels, out Button startButton, out Button englishButton, out Button chineseButton, out Button swedishButton)
        {
            Image background = CreateImage("MenuBackground", root, BackgroundColor);
            SetRect(background.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 20f), new Vector2(-20f, -20f));
            background.raycastTarget = true;

            TMP_Text title = CreateText("TitleText", root, "Welcome to Wonderland Park", 42f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(780f, 58f));
            AddLabel(labels, title, "Welcome to Wonderland Park", "\u6b22\u8fce\u6765\u5230 Wonderland Park", "V\u00e4lkommen till Wonderland Park");

            const string subtitleEn = "Choose a language, then take a quick look at the controls.\nYou can practice these basics while this guide stays open.\nPress Start when you feel ready.";
            const string subtitleZh = "\u9009\u62e9\u8bed\u8a00\uff0c\u7136\u540e\u5feb\u901f\u4e86\u89e3\u64cd\u4f5c\u3002\n\u8fd9\u4e2a\u6307\u5357\u4fdd\u6301\u6253\u5f00\u65f6\uff0c\u53ef\u4ee5\u5148\u7ec3\u4e60\u8fd9\u4e9b\u57fa\u672c\u64cd\u4f5c\u3002\n\u51c6\u5907\u597d\u4e86\u518d\u70b9\u5f00\u59cb\u3002";
            const string subtitleSv = "V\u00e4lj spr\u00e5k och ta en snabb titt p\u00e5 kontrollerna.\nDu kan \u00f6va p\u00e5 grunderna medan guiden \u00e4r \u00f6ppen.\nTryck Start n\u00e4r du \u00e4r redo.";
            TMP_Text subtitle = CreateText("SubtitleText", root, subtitleEn, 20f, FontStyles.Normal, TextAlignmentOptions.Center, BodyTextColor);
            SetAnchored(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -124f), new Vector2(780f, 68f));
            AddLabel(labels, subtitle, subtitleEn, subtitleZh, subtitleSv);

            Image accent = CreateImage("AccentLine", root, AccentColor);
            SetAnchored(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(720f, 2f));
            accent.raycastTarget = false;

            AddControlRow(root, labels, 90f, "MOVE", "\u79fb\u52a8", "R\u00f6relse", "Left stick moves. Right stick turns.", "\u5de6\u6447\u6746\u79fb\u52a8\uff0c\u53f3\u6447\u6746\u8f6c\u5411\u3002", "V\u00e4nster spak r\u00f6r dig. H\u00f6ger spak vrider.");
            AddControlRow(root, labels, 40f, "INTERACT", "\u4e92\u52a8", "Interagera", "Point with the right controller and press the right index trigger.\nEach area notice board opens more information.", "\u7528\u53f3\u624b\u63a7\u5236\u5668\u6307\u5411\u76ee\u6807\uff0c\u6309\u53f3\u624b\u98df\u6307\u952e\u4e92\u52a8\u3002\n\u6bcf\u4e2a\u533a\u57df\u7684\u516c\u544a\u724c\u90fd\u4f1a\u6253\u5f00\u66f4\u591a\u4fe1\u606f\u3002", "Peka med h\u00f6ger kontroll och tryck h\u00f6ger avtryckare.\nVarje omr\u00e5des anslagstavla \u00f6ppnar mer information.");
            AddControlRow(root, labels, -10f, "SCALE", "\u7f29\u653e", "Skala", "Right stick button: double-click to shrink, hold to grow.", "\u53f3\u6447\u6746\u6309\u952e\uff1a\u53cc\u51fb\u7f29\u5c0f\uff0c\u957f\u6309\u53d8\u5927\u3002", "H\u00f6ger spakknapp: dubbelklicka f\u00f6r att krympa, h\u00e5ll f\u00f6r att v\u00e4xa.");
            AddControlRow(root, labels, -60f, "RECENTER", "\u5bf9\u6b63", "Centrera", "Hold right B to recenter your view.", "\u957f\u6309\u53f3\u624b B \u952e\u91cd\u65b0\u5bf9\u6b63\u89c6\u89d2\u3002", "H\u00e5ll h\u00f6ger B f\u00f6r att centrera vyn.");
            AddControlRow(root, labels, -110f, "MENU", "\u83dc\u5355", "Meny", "Press the left controller Menu button for language, comfort, audio, and restart.", "\u6309\u5de6\u624b\u63a7\u5236\u5668\u7684 Menu \u952e\uff0c\u6253\u5f00\u8bed\u8a00\u3001\u8212\u9002\u5ea6\u3001\u97f3\u91cf\u548c\u91cd\u542f\u3002", "Tryck v\u00e4nster kontrolls menyknapp f\u00f6r spr\u00e5k, komfort, ljud och omstart.");

            TMP_Text languageLabel = CreateText("LanguageLabel", root, "Language", 20f, FontStyles.Bold, TextAlignmentOptions.Left, BodyTextColor);
            SetAnchored(languageLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(-305f, 132f), new Vector2(150f, 36f));
            AddLabel(labels, languageLabel, "Language", "\u8bed\u8a00", "Spr\u00e5k");

            englishButton = CreateButton("EnglishButton", root, "English", NormalButtonColor, DarkTextColor, 22f);
            chineseButton = CreateButton("ChineseButton", root, "Chinese", NormalButtonColor, DarkTextColor, 22f);
            swedishButton = CreateButton("SwedishButton", root, "Svenska", NormalButtonColor, DarkTextColor, 22f);
            SetAnchored(englishButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(-128f, 132f), new Vector2(150f, 42f));
            SetAnchored(chineseButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(38f, 132f), new Vector2(150f, 42f));
            SetAnchored(swedishButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(204f, 132f), new Vector2(150f, 42f));
            AddLabel(labels, englishButton.GetComponentInChildren<TMP_Text>(true), "English", "\u82f1\u8bed", "Engelska");
            AddLabel(labels, chineseButton.GetComponentInChildren<TMP_Text>(true), "Chinese", "\u4e2d\u6587", "Kinesiska");
            AddLabel(labels, swedishButton.GetComponentInChildren<TMP_Text>(true), "Svenska", "\u745e\u5178\u8bed", "Svenska");

            startButton = CreateButton("StartButton", root, "Start Exploring", PrimaryButtonColor, Color.white, 28f);
            SetAnchored(startButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(330f, 58f));
            AddLabel(labels, startButton.GetComponentInChildren<TMP_Text>(true), "Start Exploring", "\u5f00\u59cb\u63a2\u7d22", "B\u00f6rja utforska");
        }

        private static void AddControlRow(RectTransform root, List<LabelSpec> labels, float y, string tagEn, string tagZh, string tagSv, string bodyEn, string bodyZh, string bodySv)
        {
            Image band = CreateImage(tagEn + "Row", root, PanelBandColor);
            SetAnchored(band.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(760f, 42f));
            band.raycastTarget = false;

            TMP_Text tag = CreateText(tagEn + "Tag", root, tagEn, 17f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetAnchored(tag.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-300f, y), new Vector2(130f, 30f));
            AddLabel(labels, tag, tagEn, tagZh, tagSv);

            TMP_Text body = CreateText(tagEn + "Text", root, bodyEn, 20f, FontStyles.Normal, TextAlignmentOptions.Left, BodyTextColor);
            SetAnchored(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(90f, y), new Vector2(600f, 34f));
            AddLabel(labels, body, bodyEn, bodyZh, bodySv);
        }

        private static void ConfigureController(WelcomeFlowController controller, CanvasGroup canvasGroup, Button startButton, Button englishButton, Button chineseButton, Button swedishButton, List<LabelSpec> labels)
        {
            SerializedObject so = new SerializedObject(controller);
            Set(so, "canvasGroup", canvasGroup);
            Set(so, "startButton", startButton);
            Set(so, "englishButton", englishButton);
            Set(so, "chineseButton", chineseButton);
            Set(so, "swedishButton", swedishButton);
            Set(so, "locomotionProfile", Object.FindFirstObjectByType<QuestLocomotionComfortProfile>(FindObjectsInactive.Include));
            Set(so, "scaleManager", Object.FindFirstObjectByType<ScaleManager>(FindObjectsInactive.Include));
            Set(so, "gameFlow", Object.FindFirstObjectByType<GameFlowManager>(FindObjectsInactive.Include));
            Set(so, "distanceFromCamera", 1.6f);
            Set(so, "cameraLocalOffset", new Vector3(0f, -0.06f, 0f));
            Set(so, "panelWorldScale", new Vector3(0.00135f, 0.00135f, 0.00135f));
            Set(so, "lockLocomotionWhileShown", false);
            Set(so, "disableThumbstickScaleWhileShown", false);

            SerializedProperty labelsProperty = so.FindProperty("labels");
            labelsProperty.arraySize = labels.Count;
            for (int i = 0; i < labels.Count; i++)
            {
                LabelSpec spec = labels[i];
                SerializedProperty item = labelsProperty.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("target").objectReferenceValue = spec.target;
                item.FindPropertyRelative("english").stringValue = spec.english;
                item.FindPropertyRelative("chineseSimplified").stringValue = spec.chinese;
                item.FindPropertyRelative("swedish").stringValue = spec.swedish;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject EnsureUiSystemRoot()
        {
            GameObject systemRoot = GameObject.Find("WW_UI_System");
            if (systemRoot == null)
            {
                systemRoot = new GameObject("WW_UI_System");
            }

            if (systemRoot.GetComponent<UILanguageService>() == null)
            {
                systemRoot.AddComponent<UILanguageService>();
            }

            return systemRoot;
        }

        private static void EnsureProjectCjkFontImported()
        {
            AssetDatabase.ImportAsset(CjkFontPath, ImportAssetOptions.ForceUpdate);
            LocalizedUIFontProvider.ClearCachedFontForEditor();
        }

        private static void DestroyExistingWelcomePanels()
        {
            WelcomeFlowController[] controllers = Object.FindObjectsByType<WelcomeFlowController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null && !EditorUtility.IsPersistent(controllers[i]))
                {
                    Object.DestroyImmediate(controllers[i].gameObject);
                }
            }

            GameObject named = GameObject.Find("WelcomePanel");
            if (named != null && !EditorUtility.IsPersistent(named))
            {
                Object.DestroyImmediate(named);
            }
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(14f, fontSize - 8f);
            text.fontSizeMax = fontSize;
            text.textWrappingMode = TextWrappingModes.Normal;

            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color backgroundColor, Color textColor, float fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.color = backgroundColor;
            image.raycastTarget = true;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.78f, 0.94f, 1f, 1f);
            colors.pressedColor = new Color(0.68f, 0.78f, 0.82f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TMP_Text text = CreateText("Label", go.transform, label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, textColor);
            Stretch(text.rectTransform, new Vector2(12f, 4f), new Vector2(-12f, -4f));
            return button;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void AddLabel(List<LabelSpec> labels, TMP_Text target, string english, string chinese, string swedish)
        {
            if (target == null)
            {
                return;
            }

            LocalizedUIText localized = target.GetComponent<LocalizedUIText>();
            if (localized == null)
            {
                localized = target.gameObject.AddComponent<LocalizedUIText>();
            }

            localized.SetTexts(english, chinese, swedish);
            labels.Add(new LabelSpec { target = target, english = english, chinese = chinese, swedish = swedish });
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void Set(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void Set(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void Set(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void Set(SerializedObject serializedObject, string propertyName, Vector3 value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.vector3Value = value;
            }
        }
    }
}
