using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LotusSongUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LotusSongManager songManager;

    [Header("Song List")]
    [SerializeField] private List<LotusSongData> songs = new List<LotusSongData>();

    [Header("UI")]
    [SerializeField] private Transform songListRoot;
    [SerializeField] private Button songButtonTemplate;
    [SerializeField] private Button startButton;
    [SerializeField] private Button freePlayButton;
    [SerializeField] private TextMeshProUGUI selectedSongLabel;

    private LotusSongData selectedSong;
    private readonly List<Button> spawnedButtons = new List<Button>();

    public void Initialize(
        LotusSongManager manager,
        List<LotusSongData> songAssets,
        Transform listRoot,
        Button buttonTemplate,
        Button start,
        Button freePlay,
        TextMeshProUGUI selectedLabel)
    {
        songManager = manager;
        songs = songAssets ?? new List<LotusSongData>();
        songListRoot = listRoot;
        songButtonTemplate = buttonTemplate;
        startButton = start;
        freePlayButton = freePlay;
        selectedSongLabel = selectedLabel;
    }

    private void Reset()
    {
        songManager = FindAnyObjectByType<LotusSongManager>();
    }

    private void Awake()
    {
        if (songManager == null)
            songManager = FindAnyObjectByType<LotusSongManager>();

        if (songButtonTemplate != null)
            songButtonTemplate.gameObject.SetActive(false);

        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        if (freePlayButton != null)
            freePlayButton.onClick.AddListener(OnFreePlayClicked);
    }

    private void Start()
    {
        RebuildSongButtons();

        if (selectedSong == null && songs.Count > 0)
            SelectSong(songs[0]);
        else
            UpdateSelectedSongLabel();
    }

    public void RebuildSongButtons()
    {
        foreach (var b in spawnedButtons)
        {
            if (b != null) Destroy(b.gameObject);
        }
        spawnedButtons.Clear();

        if (songListRoot == null || songButtonTemplate == null)
            return;

        for (int i = 0; i < songs.Count; i++)
        {
            var song = songs[i];
            if (song == null)
                continue;

            var btn = Instantiate(songButtonTemplate, songListRoot);
            btn.gameObject.SetActive(true);

            var label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = string.IsNullOrWhiteSpace(song.songName) ? song.name : song.songName;

            btn.onClick.AddListener(() => SelectSong(song));
            spawnedButtons.Add(btn);
        }
    }

    public void SelectSong(LotusSongData song)
    {
        selectedSong = song;
        UpdateSelectedSongLabel();
    }

    private void UpdateSelectedSongLabel()
    {
        if (selectedSongLabel == null)
            return;

        if (selectedSong == null)
        {
            selectedSongLabel.text = "No song selected";
            return;
        }

        selectedSongLabel.text = string.IsNullOrWhiteSpace(selectedSong.songName) ? selectedSong.name : selectedSong.songName;
    }

    private void OnStartClicked()
    {
        if (songManager == null)
        {
            Debug.LogWarning("[LotusSongUI] No LotusSongManager reference.");
            return;
        }

        if (selectedSong == null)
        {
            Debug.LogWarning("[LotusSongUI] No song selected.");
            return;
        }

        songManager.StartSong(selectedSong);
    }

    private void OnFreePlayClicked()
    {
        if (songManager == null)
        {
            Debug.LogWarning("[LotusSongUI] No LotusSongManager reference.");
            return;
        }

        songManager.StopSong();
    }
}
