using UnityEngine;
using UnityEngine.InputSystem;

public class LotusRayDebugDriver : MonoBehaviour
{
    [Header("Ray")]
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private float rayDistance = 15f;
    [SerializeField] private LayerMask rayMask = Physics.DefaultRaycastLayers;

    [Header("Input")]
    [SerializeField] private bool useMouseClick = true;
    [SerializeField] private bool useKeyboardFallback = true;
    [SerializeField] private Key keyboardTriggerKey = Key.E;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRay = true;
    [SerializeField] private bool logMisses = true;

    private bool triggerPressedLastFrame;

    private void Update()
    {
        if (rayOrigin == null)
        {
            return;
        }

        bool triggerPressedThisFrame = false;

        if (useMouseClick && Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            triggerPressedThisFrame = true;
        }

        if (useKeyboardFallback && Keyboard.current != null && Keyboard.current[keyboardTriggerKey].isPressed)
        {
            triggerPressedThisFrame = true;
        }

        if (drawDebugRay)
        {
            Debug.DrawRay(rayOrigin.position, rayOrigin.forward * rayDistance, Color.cyan);
        }

        if (triggerPressedThisFrame && !triggerPressedLastFrame)
        {
            TryTriggerLotus();
        }

        triggerPressedLastFrame = triggerPressedThisFrame;
    }

    private void TryTriggerLotus()
    {
        if (!Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit, rayDistance, rayMask, QueryTriggerInteraction.Collide))
        {
            if (logMisses)
            {
                Debug.Log("LotusRayDebugDriver: raycast hit nothing.");
            }
            return;
        }

        LotusNoteTrigger lotus = hit.collider.GetComponentInParent<LotusNoteTrigger>();
        if (lotus == null)
        {
            if (logMisses)
            {
                Debug.Log($"LotusRayDebugDriver: hit {hit.collider.name}, but no LotusNoteTrigger was found.");
            }
            return;
        }

        Debug.Log($"LotusRayDebugDriver: triggering {lotus.name} from hit {hit.collider.name}.");
        lotus.TriggerNote();
    }
}
