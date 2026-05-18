using UnityEngine;
using UnityEditor;

public static class FindRenderableComponents
{
    // 菜单名改成唯一的 "(PS_AI)" 后缀，避免与工程里已有菜单冲突
    [MenuItem("Tools/Debug/Find Renderers In Selected (PS_AI)")]
    static void FindRenderers()
    {
        var selection = Selection.gameObjects;
        if (selection == null || selection.Length == 0)
        {
            Debug.LogWarning("Find Renderers: 没有选中任何 GameObject。请在 Hierarchy 里选中目标对象后重试。");
            return;
        }

        foreach (var go in selection)
        {
            Debug.Log($"--- Renderers in: {go.name} ---");
            var mrs = go.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in mrs)
            {
                Debug.Log($"MeshRenderer: {GetPath(r.gameObject)} (bounds: {r.bounds})", r.gameObject);
            }

            var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var r in srs)
            {
                Debug.Log($"SpriteRenderer: {GetPath(r.gameObject)}", r.gameObject);
            }

            var pss = go.GetComponentsInChildren<UnityEngine.ParticleSystem>(true);
            foreach (var ps in pss)
            {
                var rend = ps.GetComponent<ParticleSystemRenderer>();
                string matName = (rend != null && rend.sharedMaterial != null) ? rend.sharedMaterial.name : "None";
                string meshName = (rend != null && rend.mesh != null) ? rend.mesh.name : "None";
                string renderMode = rend != null ? rend.renderMode.ToString() : "None";
                Debug.Log($"ParticleSystem: {GetPath(ps.gameObject)} | GO={ps.gameObject.name} | Material={matName} | RenderMode={renderMode} | Mesh={meshName}", ps.gameObject);
            }
        }
    }

    // 辅助：返回 GameObject 在 Hierarchy 中的完整路径，便于定位
    static string GetPath(GameObject go)
    {
        if (go == null) return "null";
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
}