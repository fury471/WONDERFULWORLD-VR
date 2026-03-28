using UnityEngine;

namespace Wonderland.UI
{
    public class UIController : MonoBehaviour
    {
        [Header("UI Panels")]
        public GameObject onboardingPanel;
        public GameObject settingsPanel;

        void Start()
        {
            // Initial state: Show onboardingPanel, close settings.
            if (onboardingPanel != null) onboardingPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void CloseOnboarding()
        {
            if (onboardingPanel != null) onboardingPanel.SetActive(false);
            Debug.Log("UI: Onboarding Closed");
        }

        public void ToggleSettings()
        {
            if (settingsPanel != null)
            {
                bool currentState = settingsPanel.activeSelf;
                settingsPanel.SetActive(!currentState);
                Debug.Log("UI: Settings Toggled to " + !currentState);
            }
        }

    }
}