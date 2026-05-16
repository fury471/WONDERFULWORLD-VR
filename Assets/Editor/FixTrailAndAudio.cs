using UnityEngine;
using UnityEditor;
using ButterflyHouse.Butterflies;

namespace ButterflyHouse.Editor
{
    /// <summary>
    /// Helper script to fix magenta trails and set up audio quickly.
    /// </summary>
    public class FixTrailAndAudio : EditorWindow
    {
        [MenuItem("Butterfly House/Fix Trail Materials", false, 10)]
        public static void FixTrailMaterials()
        {
            // Find all Butterfly prefabs
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
            
            int fixedCount = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab == null || prefab.GetComponent<Butterfly>() == null)
                    continue;
                
                TrailRenderer trail = prefab.GetComponent<TrailRenderer>();
                if (trail == null)
                    continue;
                
                // Check if material is null or using error shader
                if (trail.sharedMaterial == null || 
                    (trail.sharedMaterial != null && trail.sharedMaterial.shader.name.Contains("Error")))
                {
                    // Create or update trail material
                    Material trailMat = CreateTrailMaterial();
                    trail.sharedMaterial = trailMat;
                    
                    // Mark prefab as dirty
                    EditorUtility.SetDirty(prefab);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(trail);
                    
                    fixedCount++;
                    Debug.Log($"Fixed trail material on: {prefab.name}");
                }
            }
            
            // Also fix trails in scene
            TrailRenderer[] sceneTrails = FindObjectsOfType<TrailRenderer>(true);
            foreach (TrailRenderer trail in sceneTrails)
            {
                if (trail.sharedMaterial == null || 
                    (trail.sharedMaterial != null && trail.sharedMaterial.shader.name.Contains("Error")))
                {
                    trail.sharedMaterial = CreateTrailMaterial();
                    fixedCount++;
                }
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log($"Fixed {fixedCount} trail material(s). Trail colors should now display correctly!");
        }
        
        private static Material CreateTrailMaterial()
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
            
            // For trails, we want to use vertex colors (which TrailRenderer provides automatically)
            // Unlit shaders should support this by default
            
            // Set color to white (trail colors come from vertex colors)
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
            
            // Enable emission for glow effect (optional)
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.white * 0.5f);
            }
            
            return mat;
        }
        
        [MenuItem("Butterfly House/Check Audio Setup", false, 11)]
        public static void CheckAudioSetup()
        {
            Debug.Log("=== Audio Setup Check ===");
            
            // Check AudioManager
            Audio.AudioManager audioManager = FindObjectOfType<Audio.AudioManager>();
            if (audioManager == null)
            {
                Debug.LogWarning("AudioManager not found in scene!");
            }
            else
            {
                SerializedObject so = new SerializedObject(audioManager);
                SerializedProperty ambienceProp = so.FindProperty("ambientClip");
                if (ambienceProp != null && ambienceProp.objectReferenceValue == null)
                {
                    Debug.LogWarning("AudioManager: Ambient Clip is not assigned. No background music will play.");
                }
                else if (ambienceProp != null && ambienceProp.objectReferenceValue != null)
                {
                    Debug.Log($"✓ AudioManager: Ambient Clip assigned ({ambienceProp.objectReferenceValue.name})");
                }
            }
            
            // Check ButterflyArchetypes
            string[] archetypeGuids = AssetDatabase.FindAssets("t:ButterflyArchetype");
            int withAudio = 0;
            int withoutAudio = 0;
            
            foreach (string guid in archetypeGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ButterflyArchetype arch = AssetDatabase.LoadAssetAtPath<ButterflyArchetype>(path);
                
                if (arch == null) continue;
                
                SerializedObject archSO = new SerializedObject(arch);
                SerializedProperty audioProp = archSO.FindProperty("baseTone");
                
                if (audioProp != null && audioProp.objectReferenceValue == null)
                {
                    withoutAudio++;
                    Debug.LogWarning($"ButterflyArchetype '{arch.name}': No Base Tone assigned. Butterflies from this archetype will be silent.");
                }
                else if (audioProp != null && audioProp.objectReferenceValue != null)
                {
                    withAudio++;
                    Debug.Log($"✓ ButterflyArchetype '{arch.name}': Base Tone assigned ({audioProp.objectReferenceValue.name})");
                }
            }
            
            Debug.Log($"\nSummary: {withAudio} archetype(s) with audio, {withoutAudio} without audio.");
            
            if (withoutAudio > 0)
            {
                Debug.LogWarning("\nTo add audio:");
                Debug.LogWarning("1. Import AudioClips to Assets/Audio/Butterflies/");
                Debug.LogWarning("2. Select ButterflyArchetype in Project window");
                Debug.LogWarning("3. In Inspector, drag AudioClip to 'Base Tone' field");
            }
            
            Debug.Log("=== End Audio Check ===");
        }
    }
}

