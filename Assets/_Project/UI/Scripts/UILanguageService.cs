using System;
using System.Collections;
using UnityEngine;

namespace Wonderland.UI
{
    [DisallowMultipleComponent]
    public sealed class UILanguageService : MonoBehaviour
    {
        private const string PlayerPrefsKey = "WonderfulWorld.UI.Language";

        public static UILanguageService Instance { get; private set; }
        public static event Action<UILanguage> LanguageChanged;

        [SerializeField] private UILanguage defaultLanguage = UILanguage.English;
        [SerializeField] private bool persistSelection = true;

        private Coroutine deferredSaveCoroutine;
        private bool hasPendingSave;

        public UILanguage CurrentLanguage { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            CurrentLanguage = LoadLanguage();
        }

        private void Start()
        {
            LanguageChanged?.Invoke(CurrentLanguage);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                FlushPendingSave();
                Instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                FlushPendingSave();
            }
        }

        private void OnApplicationQuit()
        {
            FlushPendingSave();
        }

        public void SetEnglish()
        {
            SetLanguage(UILanguage.English);
        }

        public void SetChineseSimplified()
        {
            SetLanguage(UILanguage.ChineseSimplified);
        }

        public void SetSwedish()
        {
            SetLanguage(UILanguage.Swedish);
        }

        public void SetLanguage(UILanguage language)
        {
            if (CurrentLanguage == language)
            {
                LanguageChanged?.Invoke(CurrentLanguage);
                return;
            }

            CurrentLanguage = language;
            if (persistSelection)
            {
                PlayerPrefs.SetInt(PlayerPrefsKey, (int)CurrentLanguage);
                ScheduleDeferredSave();
            }

            LanguageChanged?.Invoke(CurrentLanguage);
        }

        public static UILanguage GetCurrentOrDefault()
        {
            return Instance != null ? Instance.CurrentLanguage : UILanguage.English;
        }

        private UILanguage LoadLanguage()
        {
            if (!persistSelection || !PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                return defaultLanguage;
            }

            int saved = PlayerPrefs.GetInt(PlayerPrefsKey, (int)defaultLanguage);
            return Enum.IsDefined(typeof(UILanguage), saved) ? (UILanguage)saved : defaultLanguage;
        }

        private void ScheduleDeferredSave()
        {
            hasPendingSave = true;
            if (deferredSaveCoroutine == null && isActiveAndEnabled)
            {
                deferredSaveCoroutine = StartCoroutine(SaveAfterInteractionSettles());
            }
        }

        private IEnumerator SaveAfterInteractionSettles()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(0.25f);
            FlushPendingSave();
            deferredSaveCoroutine = null;
        }

        private void FlushPendingSave()
        {
            if (!persistSelection || !hasPendingSave)
            {
                return;
            }

            PlayerPrefs.Save();
            hasPendingSave = false;
        }
    }
}
