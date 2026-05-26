using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Text;
using System.Collections.Generic;

public class InspectMaterials
{
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Project/World/Persistent/World_WonderlandPark.unity");
        GameObject target = null;
        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj.name == "ParticleFireworkShowcase" && obj.scene == scene)
            {
                target = obj;
                break;
            }
        }

        if (target == null) return;

        StringBuilder sb = new StringBuilder();
        var renderers = target.GetComponentsInChildren<ParticleSystemRenderer>(true);
        HashSet<string> mats = new HashSet<string>();
        foreach (var r in renderers)
        {
            if (r.sharedMaterial != null)
            {
                mats.Add($"{r.gameObject.name} -> {r.sharedMaterial.name} (Shader: {r.sharedMaterial.shader.name})");
            }
            else
            {
                mats.Add($"{r.gameObject.name} -> NULL MATERIAL");
            }
        }

        foreach (var m in mats) sb.AppendLine(m);
        System.IO.File.WriteAllText("particle_mats.txt", sb.ToString());
    }
}
