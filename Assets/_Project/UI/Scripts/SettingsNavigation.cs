using UnityEngine;

namespace Wonderland.UI
{
    public sealed class SettingsNavigation : MonoBehaviour
    {
        private VRSettingsMenuView runtimeView;

        private void Awake()
        {
            runtimeView = GetComponent<VRSettingsMenuView>();
            if (runtimeView == null)
            {
                runtimeView = GetComponentInParent<VRSettingsMenuView>(true);
            }
        }

        public void ShowGeneral() => ShowPanel();
        public void ShowComfort() => ShowPanel();
        public void ShowAudio() => ShowPanel();

        public void ClosePanel()
        {
            if (runtimeView != null)
            {
                runtimeView.Cancel();
                return;
            }

            gameObject.SetActive(false);
        }

        public void ShowPanel()
        {
            gameObject.SetActive(true);
            if (runtimeView != null)
            {
                runtimeView.ShowSettingsPage();
            }
        }
    }
}
