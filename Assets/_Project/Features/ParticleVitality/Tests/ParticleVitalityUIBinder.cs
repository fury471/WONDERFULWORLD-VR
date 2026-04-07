using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ParticleVitalityUIBinder : MonoBehaviour
{
    [Header("System References")]
    [SerializeField] private ParticleCollector collector;
    [SerializeField] private ParticleShapeSystem shapeSystem;

    [Header("Collect Prompt UI")]
    [SerializeField] private GameObject collectPromptRoot;
    [SerializeField] private TMP_Text collectPromptText;
    [SerializeField] private TMP_Text collectPromptCountdownText;
    [SerializeField] private Button collectConfirmButton;
    [SerializeField] private Button collectDismissButton;

    [Header("Warning UI")]
    [SerializeField] private GameObject warningRoot;
    [SerializeField] private TMP_Text warningText;

    [Header("Shape Selection UI")]
    [SerializeField] private GameObject shapeSelectionRoot;
    [SerializeField] private Button holdCloudButton;
    [SerializeField] private Button sphereButton;
    [SerializeField] private Button heartButton;
    [SerializeField] private Button spiralButton;

    [Header("Post Collect UI")]
    [SerializeField] private GameObject postCollectRoot;
    [SerializeField] private Button keepDisplayingButton;
    [SerializeField] private Button clearParticlesButton;

    private void Awake()
    {
        BindButtons();
    }

    private void OnEnable()
    {
        RefreshUi();
    }

    private void Update()
    {
        RefreshUi();
    }

    private void BindButtons()
    {
        RegisterButton(collectConfirmButton, HandleCollectConfirmClicked);
        RegisterButton(collectDismissButton, HandleCollectDismissClicked);

        RegisterButton(holdCloudButton, () => HandleShapeSelected(ParticleShapeSystem.ShapeType.HoldCloud));
        RegisterButton(sphereButton, () => HandleShapeSelected(ParticleShapeSystem.ShapeType.Sphere));
        RegisterButton(heartButton, () => HandleShapeSelected(ParticleShapeSystem.ShapeType.Heart));
        RegisterButton(spiralButton, () => HandleShapeSelected(ParticleShapeSystem.ShapeType.Spiral));

        RegisterButton(keepDisplayingButton, HandleKeepDisplayingClicked);
        RegisterButton(clearParticlesButton, HandleClearParticlesClicked);
    }

    private void RefreshUi()
    {
        RefreshCollectPromptUi();
        RefreshWarningUi();
        RefreshShapeSelectionUi();
        RefreshPostCollectUi();
    }

    private void RefreshCollectPromptUi()
    {
        bool shouldShow = collector != null && collector.ShowCollectPrompt;
        if (collectPromptRoot != null)
        {
            collectPromptRoot.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            return;
        }

        if (collectPromptText != null)
        {
            collectPromptText.text = "Particle source detected.\nHold G to absorb particles.";
        }

        if (collectPromptCountdownText != null)
        {
            collectPromptCountdownText.text = $"Hides in {collector.CollectPromptSecondsRemaining:F1}s";
        }

        if (collectConfirmButton != null)
        {
            collectConfirmButton.gameObject.SetActive(!collector.CollectPromptConfirmed);
        }
    }

    private void RefreshWarningUi()
    {
        string currentWarning = collector != null ? collector.WarningMessage : string.Empty;
        bool shouldShow = !string.IsNullOrEmpty(currentWarning);

        if (warningRoot != null)
        {
            warningRoot.SetActive(shouldShow);
        }

        if (shouldShow && warningText != null)
        {
            warningText.text = currentWarning;
        }
    }

    private void RefreshShapeSelectionUi()
    {
        bool shouldShow = shapeSystem != null && shapeSystem.IsSelectionVisible;
        if (shapeSelectionRoot != null)
        {
            shapeSelectionRoot.SetActive(shouldShow);
        }
    }

    private void RefreshPostCollectUi()
    {
        bool shouldShow = shapeSystem != null && shapeSystem.IsPostCollectOptionsVisible;
        if (postCollectRoot != null)
        {
            postCollectRoot.SetActive(shouldShow);
        }
    }

    private void HandleCollectConfirmClicked()
    {
        collector?.ConfirmCollectPrompt();
    }

    private void HandleCollectDismissClicked()
    {
        collector?.DismissCollectPrompt();
    }

    private void HandleShapeSelected(ParticleShapeSystem.ShapeType shape)
    {
        shapeSystem?.SelectShapeAndShowPostOptions(shape);
    }

    private void HandleKeepDisplayingClicked()
    {
        shapeSystem?.ContinueDisplaying();
    }

    private void HandleClearParticlesClicked()
    {
        shapeSystem?.ClearParticlesAndCloseOptions();
    }

    private static void RegisterButton(Button button, UnityEngine.Events.UnityAction callback)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(callback);
        button.onClick.AddListener(callback);
    }
}
