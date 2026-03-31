using UnityEngine;
using UnityEngine.UI;

namespace Wonderland.UI
{
    public class SettingsNavigation : MonoBehaviour
    {
       [Header("System Buttons")]
        public Button btnClose;
       [Header("Buttons")]
        public Button btnGeneral;
        public Button btnComfort;
        public Button btnAudio;
        [Header("Pages Container")]
        public GameObject pageGeneral;
        public GameObject pageComfort;
        public GameObject pageAudio;

        private void Awake()
        {

            if (btnGeneral != null) btnGeneral.onClick.AddListener(ShowGeneral);
            if (btnComfort != null) btnComfort.onClick.AddListener(ShowComfort);
            if (btnAudio != null) btnAudio.onClick.AddListener(ShowAudio);
            if (btnClose != null) btnClose.onClick.AddListener(ClosePanel);
        }

        void Start()
        {
            ShowGeneral();
        }

        public void ShowGeneral() => SwitchTo(pageGeneral);
        public void ShowComfort() => SwitchTo(pageComfort);
        public void ShowAudio() => SwitchTo(pageAudio);

        private void SwitchTo(GameObject target)
        {
            if (pageGeneral != null) pageGeneral.SetActive(false);
            if (pageComfort != null) pageComfort.SetActive(false);
            if (pageAudio != null) pageAudio.SetActive(false);

            if (target != null) target.SetActive(true);

            
        }

        public void ClosePanel()
        {
            this.gameObject.SetActive(false);
            Debug.Log("[UI] Settings Panel closed via Close Button.");
        }

        public void ShowPanel()
        {
            this.gameObject.SetActive(true);
            Debug.Log("[UI] Settings Panel Opened.");
        }
    }
}
