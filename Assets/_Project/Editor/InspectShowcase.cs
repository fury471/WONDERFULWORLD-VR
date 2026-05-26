using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Text;

public class InspectShowcase
{
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Project/World/Persistent/World_WonderlandPark.unity");
        var roots = scene.GetRootGameObjects();
        GameObject target = null;
        foreach (var root in roots)
        {
            var t = root.transform.Find("ParticleFireworkShowcase");
            if (t != null)
            {
                target = t.gameObject;
                break;
            }
            if (root.name == "ParticleFireworkShowcase")
            {
                target = root;
                break;
            }
        }

        if (target == null)
        {
            // Search all objects just in case
            foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (obj.name == "ParticleFireworkShowcase" && obj.scene == scene)
                {
                    target = obj;
                    break;
                }
            }
        }

        if (target == null)
        {
            Debug.LogError("Could not find ParticleFireworkShowcase");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Found: {target.name}");
        
        void Inspect(Transform t, string indent)
        {
            var comps = t.GetComponents<Component>();
            string compStr = "";
            foreach (var c in comps)
            {
                if (c == null) continue;
                compStr += c.GetType().Name + " ";
                if (c is MeshFilter mf && mf.sharedMesh != null) compStr += $"(Mesh:{mf.sharedMesh.name}) ";
            }
            sb.AppendLine($"{indent}- {t.name} [{compStr}]");
            foreach (Transform child in t)
            {
                Inspect(child, indent + "  ");
            }
        }
        
        Inspect(target.transform, "");
        System.IO.File.WriteAllText("showcase_inspect.txt", sb.ToString());
        Debug.Log("Inspection done!");
    }
}
