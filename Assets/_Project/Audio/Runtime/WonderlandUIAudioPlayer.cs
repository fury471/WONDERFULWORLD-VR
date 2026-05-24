using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WonderfulWorld.Audio
{
    [DisallowMultipleComponent]
    public sealed class WonderlandUIAudioPlayer : MonoBehaviour
    {
        private const string PlayerName = "WW_Audio_UIAudioPlayer";
        private const float BindInterval = 1f;

        private static WonderlandUIAudioPlayer instance;

        private readonly List<Button> buttonBuffer = new List<Button>(64);
        private float nextBindTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
            SceneManager.sceneLoaded += (_, _) => EnsureInstance().BindButtons();
        }

        private static WonderlandUIAudioPlayer EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject existing = GameObject.Find(PlayerName);
            if (existing != null)
            {
                instance = existing.GetComponent<WonderlandUIAudioPlayer>();
            }

            if (instance == null)
            {
                GameObject go = new GameObject(PlayerName);
                DontDestroyOnLoad(go);
                instance = go.AddComponent<WonderlandUIAudioPlayer>();
            }

            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            BindButtons();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextBindTime)
            {
                return;
            }

            nextBindTime = Time.unscaledTime + BindInterval;
            BindButtons();
        }

        private void BindButtons()
        {
            buttonBuffer.Clear();
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            buttonBuffer.AddRange(FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None));
#else
#pragma warning disable CS0618
            buttonBuffer.AddRange(FindObjectsOfType<Button>(true));
#pragma warning restore CS0618
#endif

            for (int i = 0; i < buttonBuffer.Count; i++)
            {
                Button button = buttonBuffer[i];
                if (button == null || button.GetComponent<WonderlandUIButtonAudioBinding>() != null)
                {
                    continue;
                }

                WonderlandUIButtonAudioBinding binding = button.gameObject.AddComponent<WonderlandUIButtonAudioBinding>();
                binding.Configure(button);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class WonderlandUIButtonAudioBinding : MonoBehaviour, IPointerEnterHandler
    {
        private const float HoverCooldownSeconds = 0.05f;

        private Button button;
        private float nextHoverTime;

        public void Configure(Button owner)
        {
            button = owner;
            button.onClick.AddListener(PlayClick);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayClick);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isActiveAndEnabled || Time.unscaledTime < nextHoverTime)
            {
                return;
            }

            nextHoverTime = Time.unscaledTime + HoverCooldownSeconds;
            WonderlandAudioOneShotPlayer.Play2D("WW_UI_Hover", volumeScale: 1f, maxVoices: 3);
        }

        private void PlayClick()
        {
            WonderlandAudioOneShotPlayer.Play2D("WW_UI_Click", volumeScale: 1f, maxVoices: 4);
        }
    }
}
