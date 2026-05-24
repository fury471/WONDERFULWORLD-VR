using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public static class FindRenderableComponents
{
    [MenuItem("Tools/Debug/Find Renderers In Selected")]
    static void FindRenderers()
    {
        foreach (var go in Selection.gameObjects)
        {
            Debug.Log($"--- Renderers in: {go.name} ---");
            var mrs = go.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in mrs) Debug.Log($"MeshRenderer: {r.gameObject.name} (bounds: {r.bounds})", r.gameObject);
            var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var r in srs) Debug.Log($"SpriteRenderer: {r.gameObject.name}", r.gameObject);
            var pss = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pss)
            {
                var rend = ps.GetComponent<ParticleSystemRenderer>();
                if (rend != null) Debug.Log($"ParticleSystemRenderer: {ps.gameObject.name} Mode={rend.renderMode} Mesh={rend.mesh} Material={(rend.sharedMaterial ? rend.sharedMaterial.name : "None")}", ps.gameObject);
            }
        }
    }
}
#endif
