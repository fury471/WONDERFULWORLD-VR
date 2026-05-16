using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ButterflyHouse.Core;
using ButterflyHouse.Butterflies;
using ButterflyHouse.Plants;
using ButterflyHouse.Interaction;
using ButterflyHouse.Audio;
using Core = ButterflyHouse.Core;

namespace ButterflyHouse.Editor
{
    /// <summary>
    /// Editor script to automatically set up the Butterfly House sample scene.
    /// Creates all necessary managers, objects, and prefabs.
    /// </summary>
    public class ButterflyHouseSceneSetup : EditorWindow
    {
        /// <summary>
        /// Helper method to find a URP shader, with fallbacks for built-in render pipeline.
        /// </summary>
        private static Shader FindShader(string[] urpNames, string builtInName)
        {
            foreach (string name in urpNames)
            {
                Shader shader = Shader.Find(name);
                if (shader != null)
                {
                    Debug.Log($"Found shader: {name}");
                    return shader;
                }
            }
            
            // Fallback to built-in
            Shader builtInShader = Shader.Find(builtInName);
            if (builtInShader != null)
            {
                Debug.Log($"Using fallback shader: {builtInName}");
                return builtInShader;
            }
            
            Debug.LogWarning($"Could not find any shader! Tried: {string.Join(", ", urpNames)}, and fallback: {builtInName}");
            return null;
        }
        
        /// <summary>
        /// Get URP Lit shader with fallback to Standard.
        /// </summary>
        private static Shader GetLitShader()
        {
            return FindShader(new string[]
            {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Shader Graphs/Lit",
                "URP/Lit"
            }, "Standard");
        }
        
        /// <summary>
        /// Get URP Unlit shader with fallback to Unlit/Color.
        /// </summary>
        private static Shader GetUnlitShader()
        {
            return FindShader(new string[]
            {
                "Universal Render Pipeline/Unlit",
                "Shader Graphs/Unlit",
                "URP/Unlit"
            }, "Unlit/Color");
        }
        
        /// <summary>
        /// Create a material with proper error handling.
        /// </summary>
        private static Material CreateMaterial(Shader shader, string name = "Material")
        {
            if (shader == null)
            {
                Debug.LogError($"Cannot create material '{name}': shader is null! Materials will appear magenta.");
                // Create a default material that won't work but won't crash
                return new Material(Shader.Find("Hidden/InternalErrorShader"));
            }
            
            return new Material(shader) { name = name };
        }
        
        /// <summary>
        /// Create a trail material with proper URP shader for trails.
        /// </summary>
        private static Material CreateTrailMaterialForEditor()
        {
            // Try URP Unlit shader first (best for trails)
            Shader trailShader = Shader.Find("Universal Render Pipeline/Unlit") ??
                                 Shader.Find("Unlit/Color") ??
                                 Shader.Find("Sprites/Default");
            
            if (trailShader == null)
            {
                Debug.LogWarning("Could not find suitable trail shader. Trail may appear magenta.");
                trailShader = Shader.Find("Hidden/InternalErrorShader");
            }
            
            Material mat = new Material(trailShader);
            mat.name = "TrailMaterial";
            
            // Set color to white (trail colors come from vertex colors)
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
            
            return mat;
        }
        [MenuItem("Butterfly House/Setup Sample Scene", false, 1)]
        public static void SetupScene()
        {
            // Create new scene or use current
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Set up camera for MR/VR (basic setup - adjust as needed)
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0, 1.6f, 0); // Eye height
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            }
            
            // Create GameController
            GameObject gameControllerObj = new GameObject("GameController");
            GameController gameController = gameControllerObj.AddComponent<GameController>();
            
            // Create ButterflyManager
            GameObject butterflyManagerObj = new GameObject("ButterflyManager");
            ButterflyManager butterflyManager = butterflyManagerObj.AddComponent<ButterflyManager>();
            
            // Configure ButterflyManager bounding box via SerializedObject
            SerializedObject butterflyManagerSO = new SerializedObject(butterflyManager);
            SerializedProperty useBoundingBoxProp = butterflyManagerSO.FindProperty("useBoundingBox");
            if (useBoundingBoxProp != null)
            {
                useBoundingBoxProp.boolValue = true;
            }
            SerializedProperty boundingBoxMinProp = butterflyManagerSO.FindProperty("boundingBoxMin");
            if (boundingBoxMinProp != null)
            {
                boundingBoxMinProp.vector3Value = new Vector3(-10f, 0f, -10f);
            }
            SerializedProperty boundingBoxMaxProp = butterflyManagerSO.FindProperty("boundingBoxMax");
            if (boundingBoxMaxProp != null)
            {
                boundingBoxMaxProp.vector3Value = new Vector3(10f, 5f, 10f);
            }
            SerializedProperty boundarySteerStrengthProp = butterflyManagerSO.FindProperty("boundarySteerStrength");
            if (boundarySteerStrengthProp != null)
            {
                boundarySteerStrengthProp.floatValue = 2f;
            }
            SerializedProperty boundaryBufferZoneProp = butterflyManagerSO.FindProperty("boundaryBufferZone");
            if (boundaryBufferZoneProp != null)
            {
                boundaryBufferZoneProp.floatValue = 1f;
            }
            SerializedProperty surfaceAvoidanceStrengthProp = butterflyManagerSO.FindProperty("surfaceAvoidanceStrength");
            if (surfaceAvoidanceStrengthProp != null)
            {
                surfaceAvoidanceStrengthProp.floatValue = 5f;
            }
            SerializedProperty groundUpwardBiasProp = butterflyManagerSO.FindProperty("groundUpwardBias");
            if (groundUpwardBiasProp != null)
            {
                groundUpwardBiasProp.floatValue = 3f;
            }
            butterflyManagerSO.ApplyModifiedProperties();
            
            // Create PlantManager
            GameObject plantManagerObj = new GameObject("PlantManager");
            Plants.PlantManager plantManager = plantManagerObj.AddComponent<Plants.PlantManager>();
            
            // Create FruitManager
            GameObject fruitManagerObj = new GameObject("FruitManager");
            Plants.FruitManager fruitManager = fruitManagerObj.AddComponent<Plants.FruitManager>();
            
            // Create AudioManager
            GameObject audioManagerObj = new GameObject("AudioManager");
            AudioManager audioManager = audioManagerObj.AddComponent<AudioManager>();
            
            // Create InteractionManager
            GameObject interactionManagerObj = new GameObject("InteractionManager");
            InteractionManager interactionManager = interactionManagerObj.AddComponent<InteractionManager>();
            
            // Create EcosystemStateController (new spec system)
            GameObject ecosystemStateObj = new GameObject("EcosystemStateController");
            Core.EcosystemStateController ecosystemStateController = ecosystemStateObj.AddComponent<Core.EcosystemStateController>();
            
            // Create ProgressionStageManager
            GameObject stageManagerObj = new GameObject("ProgressionStageManager");
            Core.ProgressionStageManager stageManager = stageManagerObj.AddComponent<Core.ProgressionStageManager>();
            stageManagerObj.transform.SetParent(ecosystemStateObj.transform);
            
            // Create HandAuraSystem
            GameObject handAuraObj = new GameObject("HandAuraSystem");
            Core.HandAuraSystem handAuraSystem = handAuraObj.AddComponent<Core.HandAuraSystem>();
            handAuraObj.transform.SetParent(ecosystemStateObj.transform);
            
            // Create EventOrchestrator
            GameObject eventOrchestratorObj = new GameObject("EventOrchestrator");
            Core.EventOrchestrator eventOrchestrator = eventOrchestratorObj.AddComponent<Core.EventOrchestrator>();
            eventOrchestratorObj.transform.SetParent(ecosystemStateObj.transform);
            
            // Create LightCycle
            GameObject lightCycleObj = new GameObject("LightCycle");
            Core.LightCycle lightCycle = lightCycleObj.AddComponent<Core.LightCycle>();
            lightCycleObj.transform.SetParent(ecosystemStateObj.transform);
            
            // Create EcosystemOrchestrator (central brain)
            GameObject orchestratorObj = new GameObject("EcosystemOrchestrator");
            Core.EcosystemOrchestrator orchestrator = orchestratorObj.AddComponent<Core.EcosystemOrchestrator>();
            
            // Link EcosystemOrchestrator to all subsystems
            SerializedObject orchestratorSO = new SerializedObject(orchestrator);
            SerializedProperty ecosystemStateProp = orchestratorSO.FindProperty("ecosystemState");
            if (ecosystemStateProp != null)
            {
                ecosystemStateProp.objectReferenceValue = ecosystemStateController;
            }
            SerializedProperty progressionStageManagerProp = orchestratorSO.FindProperty("progressionStageManager");
            if (progressionStageManagerProp != null)
            {
                progressionStageManagerProp.objectReferenceValue = stageManager;
            }
            SerializedProperty butterflyManagerProp = orchestratorSO.FindProperty("butterflyManager");
            if (butterflyManagerProp != null)
            {
                butterflyManagerProp.objectReferenceValue = butterflyManager;
            }
            SerializedProperty fruitManagerProp = orchestratorSO.FindProperty("fruitManager");
            if (fruitManagerProp != null)
            {
                fruitManagerProp.objectReferenceValue = fruitManager;
            }
            SerializedProperty plantManagerProp = orchestratorSO.FindProperty("plantManager");
            if (plantManagerProp != null)
            {
                plantManagerProp.objectReferenceValue = plantManager;
            }
            SerializedProperty handAuraManagerProp = orchestratorSO.FindProperty("handAuraManager");
            if (handAuraManagerProp != null)
            {
                handAuraManagerProp.objectReferenceValue = handAuraSystem;
            }
            SerializedProperty eventOrchestratorProp = orchestratorSO.FindProperty("eventOrchestrator");
            if (eventOrchestratorProp != null)
            {
                eventOrchestratorProp.objectReferenceValue = eventOrchestrator;
            }
            
            // Set up dummy hand transforms if not in VR (will be replaced by actual hand tracking)
            GameObject headTransformObj = new GameObject("HeadTransform");
            headTransformObj.transform.SetParent(orchestratorObj.transform);
            SerializedProperty headTransformProp = orchestratorSO.FindProperty("headTransform");
            if (headTransformProp != null)
            {
                headTransformProp.objectReferenceValue = mainCamera != null ? mainCamera.transform : headTransformObj.transform;
            }
            
            GameObject leftHandObj = new GameObject("LeftHandTransform");
            leftHandObj.transform.SetParent(orchestratorObj.transform);
            leftHandObj.transform.position = new Vector3(-0.3f, 1.5f, 0.5f);
            SerializedProperty leftHandProp = orchestratorSO.FindProperty("leftHandTransform");
            if (leftHandProp != null)
            {
                leftHandProp.objectReferenceValue = leftHandObj.transform;
            }
            
            GameObject rightHandObj = new GameObject("RightHandTransform");
            rightHandObj.transform.SetParent(orchestratorObj.transform);
            rightHandObj.transform.position = new Vector3(0.3f, 1.5f, 0.5f);
            SerializedProperty rightHandProp = orchestratorSO.FindProperty("rightHandTransform");
            if (rightHandProp != null)
            {
                rightHandProp.objectReferenceValue = rightHandObj.transform;
            }
            
            orchestratorSO.ApplyModifiedProperties();
            
            // Link systems
            SerializedObject ecosystemSO = new SerializedObject(ecosystemStateController);
            SerializedProperty handAuraProp = ecosystemSO.FindProperty("handAuraSystem");
            if (handAuraProp != null)
            {
                handAuraProp.objectReferenceValue = handAuraSystem;
            }
            SerializedProperty eventOrchProp = ecosystemSO.FindProperty("eventOrchestrator");
            if (eventOrchProp != null)
            {
                eventOrchProp.objectReferenceValue = eventOrchestrator;
            }
            SerializedProperty stageManagerProp = ecosystemSO.FindProperty("stageManager");
            if (stageManagerProp != null)
            {
                stageManagerProp.objectReferenceValue = stageManager;
            }
            SerializedProperty lightCycleProp = ecosystemSO.FindProperty("lightCycle");
            if (lightCycleProp != null)
            {
                lightCycleProp.objectReferenceValue = lightCycle;
            }
            ecosystemSO.ApplyModifiedProperties();
            
            // Link stage manager to ecosystem state controller
            SerializedObject stageSO = new SerializedObject(stageManager);
            SerializedProperty stageButterflyManagerProp = stageSO.FindProperty("butterflyManager");
            if (stageButterflyManagerProp != null)
            {
                stageButterflyManagerProp.objectReferenceValue = butterflyManager;
            }
            stageSO.ApplyModifiedProperties();
            
            // Create a simple ground plane
            GameObject groundObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundObj.name = "GroundPlane";
            groundObj.transform.position = Vector3.zero;
            groundObj.transform.localScale = Vector3.one * 10f;
            
            // Create simple lighting
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.9f, 1f);
            light.intensity = 1f;
            lightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            
            // Add ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.2f, 0.3f, 0.4f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.15f, 0.15f, 0.2f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            
            // Create sample ButterflyArchetype if none exist
            CreateSampleButterflyArchetype();
            
            // Create sample Butterfly prefab
            GameObject butterflyPrefabObj = CreateButterflyPrefab();
            Butterfly butterflyPrefabComponent = butterflyPrefabObj.GetComponent<Butterfly>();
            
            // Assign to ButterflyManager using SerializedObject
            SerializedObject so = new SerializedObject(butterflyManager);
            SerializedProperty prefabProp = so.FindProperty("butterflyPrefab");
            if (prefabProp != null)
            {
                prefabProp.objectReferenceValue = butterflyPrefabComponent;
                so.ApplyModifiedProperties();
            }
            
            // Create sample Chrysalis objects
            for (int i = 0; i < 3; i++)
            {
                GameObject chrysalisObj = CreateChrysalisObject();
                chrysalisObj.transform.position = new Vector3(
                    Random.Range(-3f, 3f),
                    1.5f,
                    Random.Range(-3f, 3f)
                );
            }
            
            // Create sample GenerativePlant objects
            for (int i = 0; i < 2; i++)
            {
                GameObject plantObj = CreatePlantObject();
                plantObj.transform.position = new Vector3(
                    Random.Range(-4f, 4f),
                    0f,
                    Random.Range(-4f, 4f)
                );
            }
            
            // Create sample GenerativeFruit objects
            for (int i = 0; i < 5; i++)
            {
                GameObject fruitObj = CreateFruitObject();
                fruitObj.transform.position = new Vector3(
                    Random.Range(-5f, 5f),
                    Random.Range(0.5f, 2f),
                    Random.Range(-5f, 5f)
                );
            }
            
            // Create sample Flower objects (spawn some on plants)
            for (int i = 0; i < 3; i++)
            {
                GameObject flowerObj = CreateFlowerObject();
                flowerObj.transform.position = new Vector3(
                    Random.Range(-4f, 4f),
                    Random.Range(1f, 2f),
                    Random.Range(-4f, 4f)
                );
            }
            
            // Create sample LandingTarget objects
            for (int i = 0; i < 3; i++)
            {
                GameObject landingObj = CreateLandingTargetObject();
                landingObj.transform.position = new Vector3(
                    Random.Range(-5f, 5f),
                    Random.Range(0.5f, 2f),
                    Random.Range(-5f, 5f)
                );
            }
            
            // Create parent objects for organization
            GameObject environmentParent = new GameObject("Environment");
            groundObj.transform.SetParent(environmentParent.transform);
            
            GameObject butterfliesParent = new GameObject("Butterflies");
            foreach (GameObject child in scene.GetRootGameObjects())
            {
                if (child.name.StartsWith("Chrysalis"))
                    child.transform.SetParent(butterfliesParent.transform);
            }
            
            GameObject plantsParent = new GameObject("Plants");
            foreach (GameObject child in scene.GetRootGameObjects())
            {
                if (child.name.StartsWith("GenerativePlant"))
                    child.transform.SetParent(plantsParent.transform);
            }
            
            GameObject landingParent = new GameObject("LandingTargets");
            foreach (GameObject child in scene.GetRootGameObjects())
            {
                if (child.name.StartsWith("LandingTarget"))
                    child.transform.SetParent(landingParent.transform);
            }
            
            GameObject fruitsParent = new GameObject("Fruits");
            foreach (GameObject child in scene.GetRootGameObjects())
            {
                if (child.name.StartsWith("GenerativeFruit"))
                    child.transform.SetParent(fruitsParent.transform);
            }
            
            GameObject flowersParent = new GameObject("Flowers");
            foreach (GameObject child in scene.GetRootGameObjects())
            {
                if (child.name.StartsWith("Flower"))
                    child.transform.SetParent(flowersParent.transform);
            }
            
            // Mark scene as dirty and save
            EditorSceneManager.MarkSceneDirty(scene);
            
            Debug.Log("Butterfly House Sample Scene setup complete!");
            Debug.Log("Next steps:");
            Debug.Log("1. Create ButterflyArchetype ScriptableObjects (Create > Butterfly > Archetype)");
            Debug.Log("2. Assign ButterflyArchetype to Chrysalis objects");
            Debug.Log("3. Assign AudioClips to ButterflyArchetypes and GenerativePlants");
            Debug.Log("4. Create shaders for butterflies, plants, and chrysalises");
            Debug.Log("5. Set up XR/VR if needed");
        }
        
        private static void CreateSampleButterflyArchetype()
        {
            // Check if archetypes already exist
            string[] guids = AssetDatabase.FindAssets("t:ButterflyArchetype");
            if (guids.Length > 0)
            {
                Debug.Log($"Found {guids.Length} existing ButterflyArchetype(s). Skipping creation.");
                return;
            }
            
            // Create sample archetype
            ButterflyArchetype archetype = ScriptableObject.CreateInstance<ButterflyArchetype>();
            archetype.name = "Archetype_Sample";
            archetype.id = "sample_butterfly";
            archetype.displayName = "Sample Butterfly";
            
            // Set up gradient
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.5f, 0.8f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 0.7f, 0.9f), 0.5f),
                    new GradientColorKey(new Color(1f, 0.9f, 0.5f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            archetype.wingColorGradient = gradient;
            
            // Set up animation curves
            archetype.flapFrequencyCurve = AnimationCurve.Linear(0f, 2f, 1f, 2f);
            archetype.flightSpeedCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 0.5f);
            
            // Set other properties
            archetype.baseScale = 1f;
            archetype.basePitch = 1f;
            archetype.audioVolume = 0.6f;
            archetype.minFlightRadius = 2f;
            archetype.maxFlightRadius = 8f;
            archetype.noiseScale = 0.5f;
            archetype.turnSpeed = 2f;
            archetype.lifetime = 60f;
            archetype.landingInterval = 15f;
            
            // Save to assets
            string path = "Assets/ScriptableObjects/Butterflies/Archetype_Sample.asset";
            if (!System.IO.Directory.Exists("Assets/ScriptableObjects/Butterflies"))
            {
                System.IO.Directory.CreateDirectory("Assets/ScriptableObjects/Butterflies");
            }
            
            AssetDatabase.CreateAsset(archetype, path);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Created sample ButterflyArchetype at {path}");
        }
        
        private static GameObject CreateButterflyPrefab()
        {
            GameObject butterflyObj = new GameObject("Butterfly");
            
            // Add mesh (simple quad for now)
            GameObject wingObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wingObj.name = "Wing";
            wingObj.transform.SetParent(butterflyObj.transform);
            wingObj.transform.localPosition = Vector3.zero;
            wingObj.transform.localScale = Vector3.one * 0.5f;
            
            // Add components
            Butterfly butterfly = butterflyObj.AddComponent<Butterfly>();
            ButterflyVisualController visual = butterflyObj.AddComponent<ButterflyVisualController>();
            AudioSource audioSource = butterflyObj.AddComponent<AudioSource>();
            ButterflyAudio audio = butterflyObj.AddComponent<ButterflyAudio>();
            
            // Add ButterflyFormEvolution
            ButterflyFormEvolution formEvolution = butterflyObj.AddComponent<ButterflyFormEvolution>();
            
            // Configure debug settings - disable debug logs by default
            SerializedObject butterflySO = new SerializedObject(butterfly);
            SerializedProperty enableDebugLogsProp = butterflySO.FindProperty("enableDebugLogs");
            if (enableDebugLogsProp != null)
            {
                enableDebugLogsProp.boolValue = false; // Disabled by default
                butterflySO.ApplyModifiedProperties();
            }
            
            // Configure AudioSource
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 1f;
            
            // Add TrailRenderer
            TrailRenderer trail = butterflyObj.AddComponent<TrailRenderer>();
            trail.time = 2f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0f;
            
            // Create trail material with URP-compatible shader
            Material trailMat = CreateTrailMaterialForEditor();
            trail.material = trailMat;
            
            trail.startColor = Color.white;
            trail.endColor = new Color(1f, 1f, 1f, 0f);
            
            // Add collider
            SphereCollider collider = butterflyObj.AddComponent<SphereCollider>();
            collider.radius = 0.1f;
            collider.isTrigger = true;
            
            // Save as prefab
            string prefabPath = "Assets/Prefabs/Butterfly.prefab";
            if (!System.IO.Directory.Exists("Assets/Prefabs"))
            {
                System.IO.Directory.CreateDirectory("Assets/Prefabs");
            }
            
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(butterflyObj, prefabPath);
            DestroyImmediate(butterflyObj);
            
            Debug.Log($"Created Butterfly prefab at {prefabPath}");
            return prefab;
        }
        
        private static GameObject CreateChrysalisObject()
        {
            GameObject chrysalisObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            chrysalisObj.name = "Chrysalis";
            chrysalisObj.transform.localScale = new Vector3(0.3f, 0.5f, 0.3f);
            
            // Add Chrysalis component
            Chrysalis chrysalis = chrysalisObj.AddComponent<Chrysalis>();
            
            // Try to find and assign an archetype if one exists
            string[] guids = AssetDatabase.FindAssets("t:ButterflyArchetype");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                ButterflyArchetype archetype = AssetDatabase.LoadAssetAtPath<ButterflyArchetype>(path);
                SerializedObject chrysalisSO = new SerializedObject(chrysalis);
                SerializedProperty archetypeProp = chrysalisSO.FindProperty("archetype");
                if (archetypeProp != null)
                {
                    archetypeProp.objectReferenceValue = archetype;
                    chrysalisSO.ApplyModifiedProperties();
                }
            }
            
            // Set material to indicate it's a chrysalis
            Renderer renderer = chrysalisObj.GetComponent<Renderer>();
            // Try URP shader first, fallback to built-in if not found
            Shader chrysalisShader = GetLitShader();
            Material mat = CreateMaterial(chrysalisShader, "ChrysalisMaterial");
            mat.color = new Color(0.7f, 0.5f, 0.9f, 1f);
            // Set URP properties (these work for both URP and built-in)
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0.3f);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.6f);
            else if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", 0.6f);
            renderer.material = mat;
            
            return chrysalisObj;
        }
        
        private static GameObject CreatePlantObject()
        {
            GameObject plantObj = new GameObject("GenerativePlant");
            
            // Add mesh (cylinder for stem, sphere for top)
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Stem";
            stem.transform.SetParent(plantObj.transform);
            stem.transform.localPosition = new Vector3(0, 0.5f, 0);
            stem.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
            
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            top.name = "Top";
            top.transform.SetParent(plantObj.transform);
            top.transform.localPosition = new Vector3(0, 1.2f, 0);
            top.transform.localScale = Vector3.one * 0.8f;
            
            // Add components
            GenerativePlant plant = plantObj.AddComponent<GenerativePlant>();
            PlantVisualController visual = plantObj.AddComponent<PlantVisualController>();
            PlantGrowthSystem growthSystem = plantObj.AddComponent<PlantGrowthSystem>();
            AudioSource audioSource = plantObj.AddComponent<AudioSource>();
            
            // Configure AudioSource
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 1f;
            
            // Add colliders for touch
            SphereCollider topCollider = top.GetComponent<SphereCollider>();
            topCollider.radius = 1f;
            topCollider.isTrigger = true;
            
            // Set materials
            // Try URP shader first, fallback to built-in if not found
            Shader plantShader = GetLitShader();
            Material plantMat = CreateMaterial(plantShader, "PlantMaterial");
            plantMat.color = new Color(0.2f, 0.8f, 0.3f, 1f);
            if (plantMat.HasProperty("_Metallic"))
                plantMat.SetFloat("_Metallic", 0f);
            if (plantMat.HasProperty("_Smoothness"))
                plantMat.SetFloat("_Smoothness", 0.3f);
            else if (plantMat.HasProperty("_Glossiness"))
                plantMat.SetFloat("_Glossiness", 0.3f);
            
            stem.GetComponent<Renderer>().material = plantMat;
            top.GetComponent<Renderer>().material = plantMat;
            
            return plantObj;
        }
        
        private static GameObject CreateLandingTargetObject()
        {
            GameObject landingObj = new GameObject("LandingTarget");
            
            // Add visual indicator (sphere)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(landingObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 0.3f;
            
            // Add collider for detection
            SphereCollider collider = landingObj.AddComponent<SphereCollider>();
            collider.radius = 0.4f;
            collider.isTrigger = true;
            
            // Add LandingTarget component
            LandingTarget landingTarget = landingObj.AddComponent<LandingTarget>();
            // Target type will use default (Environment)
            
            // Set material - use URP Unlit for transparent effect
            Shader transparentShader = GetUnlitShader();
            Material mat = CreateMaterial(transparentShader, "LandingTargetMaterial");
            mat.color = new Color(0.5f, 0.8f, 1f, 0.5f);
            
            // Set transparency properties (URP vs Built-in have different properties)
            if (transparentShader != null && transparentShader.name.Contains("Universal Render Pipeline"))
            {
                // URP Unlit shader - set surface type
                if (mat.HasProperty("_Surface"))
                    mat.SetFloat("_Surface", 1); // Transparent
                if (mat.HasProperty("_Blend"))
                    mat.SetFloat("_Blend", 0); // Alpha
            }
            else
            {
                // Built-in Standard shader
                mat.SetFloat("_Mode", 3); // Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
            
            visual.GetComponent<Renderer>().material = mat;
            
            return landingObj;
        }
        
        private static GameObject CreateFruitObject()
        {
            GameObject fruitObj = new GameObject("GenerativeFruit");
            
            // Add mesh (sphere for fruit)
            GameObject fruitMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fruitMesh.name = "FruitMesh";
            fruitMesh.transform.SetParent(fruitObj.transform);
            fruitMesh.transform.localPosition = Vector3.zero;
            fruitMesh.transform.localScale = Vector3.one * 0.4f;
            
            // Add collider for touch detection (set as trigger)
            SphereCollider fruitCollider = fruitMesh.GetComponent<SphereCollider>();
            if (fruitCollider != null)
            {
                fruitCollider.isTrigger = true; // Enable trigger for touch detection
                fruitCollider.radius = 0.5f; // Slightly larger for easier touching
            }
            
            // Add components
            Plants.GenerativeFruit fruit = fruitObj.AddComponent<Plants.GenerativeFruit>();
            Plants.FruitGrowthSystem growthSystem = fruitObj.AddComponent<Plants.FruitGrowthSystem>();
            Plants.FruitVisualController visualController = fruitObj.AddComponent<Plants.FruitVisualController>();
            AudioSource audioSource = fruitObj.AddComponent<AudioSource>();
            
            // Configure AudioSource
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 1f;
            
            // Set material
            Shader fruitShader = GetLitShader();
            Material fruitMat = CreateMaterial(fruitShader, "FruitMaterial");
            
            // Random fruit color
            Color[] fruitColors = new Color[]
            {
                new Color(1f, 0.3f, 0.3f, 1f), // Red
                new Color(1f, 0.7f, 0.2f, 1f), // Orange
                new Color(1f, 0.9f, 0.3f, 1f), // Yellow
                new Color(0.8f, 0.9f, 0.3f, 1f), // Green-Yellow
                new Color(0.7f, 0.2f, 0.8f, 1f), // Purple
            };
            Color fruitColor = fruitColors[Random.Range(0, fruitColors.Length)];
            
            fruitMat.color = fruitColor;
            if (fruitMat.HasProperty("_Metallic"))
                fruitMat.SetFloat("_Metallic", 0.2f);
            if (fruitMat.HasProperty("_Smoothness"))
                fruitMat.SetFloat("_Smoothness", 0.7f);
            else if (fruitMat.HasProperty("_Glossiness"))
                fruitMat.SetFloat("_Glossiness", 0.7f);
            
            fruitMesh.GetComponent<Renderer>().material = fruitMat;
            
            // Set up GenerativeFruit component via SerializedObject
            SerializedObject fruitSO = new SerializedObject(fruit);
            SerializedProperty createLandingProp = fruitSO.FindProperty("createLandingTarget");
            if (createLandingProp != null)
            {
                createLandingProp.boolValue = true;
            }
            SerializedProperty landingZoneProp = fruitSO.FindProperty("landingZoneRadius");
            if (landingZoneProp != null)
            {
                landingZoneProp.floatValue = 0.3f;
            }
            SerializedProperty animateGlowProp = fruitSO.FindProperty("animateGlow");
            if (animateGlowProp != null)
            {
                animateGlowProp.boolValue = true;
            }
            SerializedProperty glowIntensityProp = fruitSO.FindProperty("glowIntensity");
            if (glowIntensityProp != null)
            {
                glowIntensityProp.floatValue = 0.5f;
            }
            fruitSO.ApplyModifiedProperties();
            
            return fruitObj;
        }
        
        private static GameObject CreateFlowerObject()
        {
            GameObject flowerObj = new GameObject("Flower");
            
            // Add mesh (sphere for flower head)
            GameObject flowerMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flowerMesh.name = "FlowerHead";
            flowerMesh.transform.SetParent(flowerObj.transform);
            flowerMesh.transform.localPosition = Vector3.zero;
            flowerMesh.transform.localScale = Vector3.one * 0.25f;
            
            // Add collider for touch detection (set as trigger)
            SphereCollider flowerCollider = flowerMesh.GetComponent<SphereCollider>();
            if (flowerCollider != null)
            {
                flowerCollider.isTrigger = true; // Enable trigger for touch detection
                flowerCollider.radius = 0.35f; // Slightly larger for easier touching
            }
            
            // Add components
            Flowers.Flower flowerComponent = flowerObj.AddComponent<Flowers.Flower>();
            Flowers.FlowerVisualController visualController = flowerObj.AddComponent<Flowers.FlowerVisualController>();
            AudioSource audioSource = flowerObj.AddComponent<AudioSource>();
            
            // Configure AudioSource
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 1f;
            
            // Set material
            Shader flowerShader = GetLitShader();
            Material flowerMat = CreateMaterial(flowerShader, "FlowerMaterial");
            
            // Random flower color
            Color[] flowerColors = new Color[]
            {
                new Color(1f, 0.4f, 0.6f, 1f), // Pink
                new Color(1f, 0.8f, 0.2f, 1f), // Yellow
                new Color(0.8f, 0.3f, 0.9f, 1f), // Purple
                new Color(1f, 0.2f, 0.2f, 1f), // Red
                new Color(1f, 0.6f, 0.8f, 1f), // Light Pink
                new Color(0.9f, 0.9f, 0.3f, 1f), // Light Yellow
            };
            Color flowerColorValue = flowerColors[Random.Range(0, flowerColors.Length)];
            
            flowerMat.color = flowerColorValue;
            if (flowerMat.HasProperty("_Metallic"))
                flowerMat.SetFloat("_Metallic", 0.1f);
            if (flowerMat.HasProperty("_Smoothness"))
                flowerMat.SetFloat("_Smoothness", 0.5f);
            else if (flowerMat.HasProperty("_Glossiness"))
                flowerMat.SetFloat("_Glossiness", 0.5f);
            
            flowerMesh.GetComponent<Renderer>().material = flowerMat;
            
            // Set up Flower component via SerializedObject
            SerializedObject flowerSO = new SerializedObject(flowerComponent);
            SerializedProperty createLandingProp = flowerSO.FindProperty("createLandingTarget");
            if (createLandingProp != null)
            {
                createLandingProp.boolValue = true;
            }
            SerializedProperty landingZoneProp = flowerSO.FindProperty("landingZoneRadius");
            if (landingZoneProp != null)
            {
                landingZoneProp.floatValue = 0.3f;
            }
            SerializedProperty nectarValueProp = flowerSO.FindProperty("nectarValue");
            if (nectarValueProp != null)
            {
                nectarValueProp.floatValue = 0.5f;
            }
            SerializedProperty pollenYieldProp = flowerSO.FindProperty("pollenYield");
            if (pollenYieldProp != null)
            {
                pollenYieldProp.floatValue = 0.5f;
            }
            flowerSO.ApplyModifiedProperties();
            
            return flowerObj;
        }
    }
}

