using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LotusSongUIController : MonoBehaviour
{
    [SerializeField] private LotusSongManager songManager;
    [SerializeField] private LotusSongData[] songs;
    [SerializeField] private Transform songListRoot;
    [SerializeField] private Button songButtonTemplate;
    [SerializeField] private Button startButton;
    [SerializeField] private Button freePlayButton;
    [SerializeField] private TMP_Text selectedSongLabel;

    private LotusSongData selectedSong;

    private void Awake()
    {
        if (songManager == null)
        {
            songManager = FindFirstObjectByType<LotusSongManager>();
        }

        selectedSong = songs != null && songs.Length > 0 ? songs[0] : null;
        UpdateSelectedSongLabel();
        WireButtons();
    }

    public void SelectSong(LotusSongData song)
    {
        selectedSong = song;
        UpdateSelectedSongLabel();
    }

    public void StartSelectedSong()
    {
        if (songManager != null && selectedSong != null)
        {
            songManager.StartSong(selectedSong);
        }
    }

    public void EnableFreePlay()
    {
        if (songManager != null)
        {
            songManager.StopSong();
        }
    }

    private void WireButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartSelectedSong);
            startButton.onClick.AddListener(StartSelectedSong);
        }

        if (freePlayButton != null)
        {
            freePlayButton.onClick.RemoveListener(EnableFreePlay);
            freePlayButton.onClick.AddListener(EnableFreePlay);
        }

        if (songButtonTemplate != null && songs != null && songs.Length > 0)
        {
            songButtonTemplate.onClick.RemoveAllListeners();
            songButtonTemplate.onClick.AddListener(() => SelectSong(songs[0]));
        }
    }

    private void UpdateSelectedSongLabel()
    {
        if (selectedSongLabel != null)
        {
            selectedSongLabel.text = selectedSong != null ? selectedSong.songName : "Free Play";
        }
    }
}
