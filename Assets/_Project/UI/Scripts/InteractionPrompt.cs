using UnityEngine;
using TMPro;

public class InteractionPrompt : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject uiRoot;   // 你的Canvas或TMP根节点
    [SerializeField] private TMP_Text promptText; // 那行文字

    [Header("Show Rules")]
    [SerializeField] private bool usePlayerTag = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask playerLayers = ~0;

    [Header("Billboard")]
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private float yOffset = 0.0f;

    [Header("Float")]
    [SerializeField] private bool floatUpDown = true;
    [SerializeField] private float floatAmplitude = 0.03f;
    [SerializeField] private float floatSpeed = 2.0f;

    private Vector3 baseLocalPos;
    private int insideCount;

    private void Awake()
    {
        if (uiRoot != null) uiRoot.SetActive(false);
        baseLocalPos = transform.localPosition;
    }

    public void SetText(string text)
    {
        if (promptText != null) promptText.text = text;
    }

    private void LateUpdate()
    {
        if (floatUpDown)
        {
            var p = baseLocalPos;
            p.y += Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.localPosition = p;
        }

        if (faceCamera)
        {
            var cam = Camera.main != null ? Camera.main.transform : null;
            if (cam != null)
            {
                var lookPos = cam.position;
                lookPos.y = transform.position.y + yOffset;
                transform.LookAt(lookPos);
                transform.Rotate(0f, 180f, 0f);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        insideCount++;
        if (uiRoot != null) uiRoot.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        insideCount = Mathf.Max(0, insideCount - 1);
        if (insideCount == 0 && uiRoot != null) uiRoot.SetActive(false);
    }

    private bool IsPlayer(Collider other)
    {
        if (usePlayerTag) return other.CompareTag(playerTag);
        return (playerLayers.value & (1 << other.gameObject.layer)) != 0;
    }
}
