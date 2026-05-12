using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WonderfulWorld.Features.Fireworks
{
    [DisallowMultipleComponent]
    public sealed class FireworkMagicActivator : MonoBehaviour
    {
        [Header("Launch")]
        [SerializeField] private FireworkLaunchPad launchPad;
        [SerializeField] private Transform deviceTarget;
        [SerializeField] private float launchDelayAfterArrival = 2.25f;

        [Header("Interaction")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private InputActionReference interactAction;
        [SerializeField] private LayerMask interactLayers = ~0;
        [SerializeField] private float maxInteractDistance = 36f;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Production Debug")]
        [SerializeField] private bool enableKeyboardMouseDebug = true;
        [SerializeField] private bool allowMouseClickDebug = true;
        [SerializeField] private bool logDebug;

        [Header("Magic Projectile")]
        [SerializeField] private float projectileFlightSeconds = 1.55f;
        [SerializeField] private float projectileArcHeight = 2.5f;
        [SerializeField] private float projectileSideCurve = 1.15f;
        [SerializeField] private float projectileSecondarySideCurve = -0.55f;
        [SerializeField] private float trailWidth = 0.018f;
        [SerializeField] private float trailVisibleFraction = 0.38f;
        [SerializeField] private int trailSegments = 34;
        [SerializeField] private float haloWidthMultiplier = 2.35f;
        [SerializeField] private float spiralRadius = 0.12f;
        [SerializeField] private float spiralRadiusVariation = 0.08f;
        [SerializeField] private float spiralTurns = 3.15f;
        [SerializeField] private int spiralStrandCount = 3;
        [SerializeField] private float strandWidthMultiplier = 0.42f;
        [SerializeField] private float impactSparkSeconds = 0.34f;
        [SerializeField] private int impactSparkCount = 14;
        [SerializeField] private Color fireColor = new Color(1f, 0.36f, 0.06f, 1f);
        [SerializeField] private Color trailColor = new Color(1f, 0.72f, 0.18f, 0.85f);

        private readonly RaycastHit[] raycastHits = new RaycastHit[8];
        private Collider[] deviceColliders;
        private Coroutine activationRoutine;
        private Material runtimeMagicMaterial;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnDestroy()
        {
            if (runtimeMagicMaterial != null)
            {
                Destroy(runtimeMagicMaterial);
            }
        }

        private void Update()
        {
            if (activationRoutine != null || !WasInteractPressed(out bool useMouseRay))
            {
                return;
            }

            if (TryBuildInteractionRay(useMouseRay, out Ray ray) && RayHitsThisDevice(ray, out Vector3 launchOrigin))
            {
                activationRoutine = StartCoroutine(ActivateAfterMagicFlight(launchOrigin));
            }
        }

        private void CacheReferences()
        {
            if (launchPad == null)
            {
                launchPad = GetComponentInParent<FireworkLaunchPad>();
                if (launchPad == null)
                {
                    launchPad = FindFirstObjectByType<FireworkLaunchPad>();
                }
            }

            if (deviceTarget == null)
            {
                deviceTarget = transform;
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            deviceColliders = GetComponentsInChildren<Collider>(true);
        }

        private bool WasInteractPressed(out bool useMouseRay)
        {
            useMouseRay = false;

            if (interactAction != null && interactAction.action != null && interactAction.action.WasPressedThisFrame())
            {
                return true;
            }

            if (!enableKeyboardMouseDebug)
            {
                return false;
            }

            if (allowMouseClickDebug && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                useMouseRay = true;
                return true;
            }

            return false;
        }

        private bool TryBuildInteractionRay(bool useMouseRay, out Ray ray)
        {
            if (useMouseRay && playerCamera != null && Mouse.current != null)
            {
                ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                return true;
            }

            if (rayOrigin != null)
            {
                ray = new Ray(rayOrigin.position, rayOrigin.forward);
                return true;
            }

            if (playerCamera != null)
            {
                Transform cameraTransform = playerCamera.transform;
                ray = new Ray(cameraTransform.position, cameraTransform.forward);
                return true;
            }

            playerCamera = Camera.main;
            if (playerCamera != null)
            {
                Transform cameraTransform = playerCamera.transform;
                ray = new Ray(cameraTransform.position, cameraTransform.forward);
                return true;
            }

            ray = default;
            return false;
        }

        private bool RayHitsThisDevice(Ray ray, out Vector3 launchOrigin)
        {
            launchOrigin = ray.origin + ray.direction.normalized * 0.35f;

            int hitCount = Physics.RaycastNonAlloc(
                ray,
                raycastHits,
                Mathf.Max(0.1f, maxInteractDistance),
                interactLayers,
                triggerInteraction);

            Collider nearestCollider = null;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                if (raycastHits[i].distance < nearestDistance)
                {
                    nearestDistance = raycastHits[i].distance;
                    nearestCollider = raycastHits[i].collider;
                }
            }

            return IsOwnCollider(nearestCollider);
        }

        private bool IsOwnCollider(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return false;
            }

            for (int i = 0; i < deviceColliders.Length; i++)
            {
                if (deviceColliders[i] == hitCollider)
                {
                    return true;
                }
            }

            return hitCollider.transform.IsChildOf(transform);
        }

        private IEnumerator ActivateAfterMagicFlight(Vector3 launchOrigin)
        {
            if (launchPad == null)
            {
                CacheReferences();
            }

            Transform target = deviceTarget != null ? deviceTarget : transform;
            yield return FlyMagicProjectile(launchOrigin, target.position);

            if (launchDelayAfterArrival > 0f)
            {
                yield return new WaitForSeconds(launchDelayAfterArrival);
            }

            launchPad?.TriggerShowcase();
            activationRoutine = null;

            if (logDebug)
            {
                Debug.Log("[Fireworks] Magic activator triggered launch.", this);
            }
        }

        private IEnumerator FlyMagicProjectile(Vector3 start, Vector3 end)
        {
            GameObject magicRoot = new GameObject("FireworkMagicRibbon");
            Transform magicTransform = magicRoot.transform;

            LineRenderer halo = CreateMagicLine(magicRoot, "OuterFireGlow", trailWidth * haloWidthMultiplier, trailColor, 0.015f, 0.24f);
            LineRenderer core = CreateMagicLine(magicRoot, "InnerFireThread", trailWidth, fireColor, 0.035f, 0.92f);

            int strandCount = Mathf.Clamp(spiralStrandCount, 0, 6);
            LineRenderer[] strands = new LineRenderer[strandCount];
            for (int i = 0; i < strandCount; i++)
            {
                strands[i] = CreateMagicLine(
                    magicRoot,
                    $"SpiralFireThread_{i + 1}",
                    trailWidth * strandWidthMultiplier,
                    Color.Lerp(trailColor, fireColor, 0.35f),
                    0f,
                    0.72f);
            }

            Light glow = magicRoot.AddComponent<Light>();
            glow.color = fireColor;
            glow.intensity = 1.25f;
            glow.range = 1.65f;

            Vector3 travel = end - start;
            Vector3 travelDirection = travel.sqrMagnitude > 0.0001f ? travel.normalized : Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, travelDirection);
            if (side.sqrMagnitude < 0.0001f)
            {
                side = Vector3.right;
            }

            side.Normalize();
            Vector3 controlA = Vector3.Lerp(start, end, 0.32f) + Vector3.up * (projectileArcHeight * 0.62f) + side * projectileSideCurve;
            Vector3 controlB = Vector3.Lerp(start, end, 0.74f) + Vector3.up * projectileArcHeight + side * projectileSecondarySideCurve;
            float duration = Mathf.Max(0.05f, projectileFlightSeconds);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                Vector3 current = CubicBezier(start, controlA, controlB, end, eased);

                magicTransform.position = current;
                UpdateRibbonLine(core, start, controlA, controlB, end, eased, 0f, 0f, false);
                UpdateRibbonLine(halo, start, controlA, controlB, end, eased, 0f, 0f, false);

                for (int i = 0; i < strands.Length; i++)
                {
                    float phase = i / Mathf.Max(1f, strands.Length) * Mathf.PI * 2f;
                    UpdateRibbonLine(strands[i], start, controlA, controlB, end, eased, phase, spiralRadius, true);
                }

                glow.intensity = Mathf.Lerp(0.9f, 1.9f, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            magicTransform.position = end;
            UpdateRibbonLine(core, start, controlA, controlB, end, 1f, 0f, 0f, false);
            UpdateRibbonLine(halo, start, controlA, controlB, end, 1f, 0f, 0f, false);
            for (int i = 0; i < strands.Length; i++)
            {
                float phase = i / Mathf.Max(1f, strands.Length) * Mathf.PI * 2f;
                UpdateRibbonLine(strands[i], start, controlA, controlB, end, 1f, phase, spiralRadius, true);
            }

            yield return PlayImpactSparks(magicRoot, end, (end - start).normalized);
            Destroy(magicRoot);
        }

        private LineRenderer CreateMagicLine(GameObject parent, string lineName, float width, Color color, float startAlpha, float endAlpha)
        {
            GameObject lineObject = new GameObject(lineName);
            lineObject.transform.SetParent(parent.transform, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = GetRuntimeMagicMaterial();
            line.positionCount = Mathf.Max(6, trailSegments);
            line.widthMultiplier = Mathf.Max(0.006f, width);
            line.numCapVertices = 4;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.widthCurve = CreateTailTaperCurve();
            line.startColor = new Color(color.r, color.g, color.b, startAlpha);
            line.endColor = new Color(color.r, color.g, color.b, endAlpha);
            return line;
        }

        private IEnumerator PlayImpactSparks(GameObject parent, Vector3 impactPoint, Vector3 incomingDirection)
        {
            int sparkCount = Mathf.Clamp(impactSparkCount, 0, 32);
            if (sparkCount == 0 || impactSparkSeconds <= 0f)
            {
                yield break;
            }

            LineRenderer[] sparks = new LineRenderer[sparkCount];
            Vector3[] directions = new Vector3[sparkCount];
            float[] lengths = new float[sparkCount];

            Vector3 baseRight = Vector3.Cross(Vector3.up, incomingDirection);
            if (baseRight.sqrMagnitude < 0.0001f)
            {
                baseRight = Vector3.right;
            }

            baseRight.Normalize();
            Vector3 baseUp = Vector3.Cross(incomingDirection, baseRight).normalized;

            for (int i = 0; i < sparkCount; i++)
            {
                float angle = i / Mathf.Max(1f, sparkCount) * Mathf.PI * 2f;
                float lift = Mathf.Lerp(0.15f, 0.75f, Halton(i + 1, 3));
                directions[i] = (baseRight * Mathf.Cos(angle) + baseUp * Mathf.Sin(angle) + Vector3.up * lift).normalized;
                lengths[i] = Mathf.Lerp(0.18f, 0.48f, Halton(i + 1, 5));
                sparks[i] = CreateMagicLine(parent, $"ImpactFireSpark_{i + 1}", trailWidth * 0.45f, fireColor, 0.92f, 0f);
                sparks[i].positionCount = 2;
            }

            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, impactSparkSeconds);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float fade = 1f - t;

                for (int i = 0; i < sparks.Length; i++)
                {
                    Vector3 tip = impactPoint + directions[i] * lengths[i] * Mathf.Sin(t * Mathf.PI * 0.85f);
                    sparks[i].startColor = new Color(fireColor.r, fireColor.g, fireColor.b, 0.85f * fade);
                    sparks[i].endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
                    sparks[i].SetPosition(0, impactPoint);
                    sparks[i].SetPosition(1, tip);
                }

                yield return null;
            }
        }

        private Material GetRuntimeMagicMaterial()
        {
            if (runtimeMagicMaterial != null)
            {
                return runtimeMagicMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Hidden/InternalErrorShader");
            }

            runtimeMagicMaterial = new Material(shader);
            runtimeMagicMaterial.renderQueue = 3000;
            if (runtimeMagicMaterial.HasProperty("_Surface"))
            {
                runtimeMagicMaterial.SetFloat("_Surface", 1f);
            }

            if (runtimeMagicMaterial.HasProperty("_Blend"))
            {
                runtimeMagicMaterial.SetFloat("_Blend", 1f);
            }

            runtimeMagicMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            runtimeMagicMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            runtimeMagicMaterial.SetFloat("_ZWrite", 0f);
            runtimeMagicMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            runtimeMagicMaterial.EnableKeyword("_ALPHABLEND_ON");

            if (runtimeMagicMaterial.HasProperty("_BaseColor"))
            {
                runtimeMagicMaterial.SetColor("_BaseColor", fireColor);
            }
            else if (runtimeMagicMaterial.HasProperty("_Color"))
            {
                runtimeMagicMaterial.SetColor("_Color", fireColor);
            }

            return runtimeMagicMaterial;
        }

        private void UpdateRibbonLine(LineRenderer trail, Vector3 start, Vector3 controlA, Vector3 controlB, Vector3 end, float visibleT, float phase, float radius, bool spiral)
        {
            int count = trail.positionCount;
            Vector3 pathForward = (end - start).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, pathForward);
            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.right;
            }

            right.Normalize();
            Vector3 up = Vector3.Cross(pathForward, right).normalized;
            float tailStart = Mathf.Clamp01(visibleT - Mathf.Clamp01(trailVisibleFraction));
            float visibleSpan = Mathf.Max(0.001f, visibleT - tailStart);

            for (int i = 0; i < count; i++)
            {
                float segmentT = i / Mathf.Max(1f, count - 1);
                float t = Mathf.Clamp01(tailStart + visibleSpan * segmentT);
                Vector3 point = CubicBezier(start, controlA, controlB, end, t);
                if (spiral)
                {
                    float taper = Mathf.Sin(segmentT * Mathf.PI);
                    float unevenRadius = radius + spiralRadiusVariation * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 5.2f + phase * 1.7f));
                    unevenRadius *= Mathf.Lerp(0.62f, 1.08f, Mathf.PerlinNoise(t * 3.1f, phase));
                    float angle = phase + t * spiralTurns * Mathf.PI * 2f + Time.time * 4.5f;
                    point += (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * unevenRadius * taper;
                }

                trail.SetPosition(i, point);
            }
        }

        private static AnimationCurve CreateTailTaperCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0.05f),
                new Keyframe(0.28f, 0.22f),
                new Keyframe(0.78f, 0.72f),
                new Keyframe(1f, 1f));
        }

        private static float Halton(int index, int radix)
        {
            float result = 0f;
            float fraction = 1f / radix;
            while (index > 0)
            {
                result += fraction * (index % radix);
                index = Mathf.FloorToInt(index / (float)radix);
                fraction /= radix;
            }

            return result;
        }

        private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * a + 2f * inverse * t * b + t * t * c;
        }

        private static Vector3 CubicBezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * inverse * a
                + 3f * inverse * inverse * t * b
                + 3f * inverse * t * t * c
                + t * t * t * d;
        }
    }
}
