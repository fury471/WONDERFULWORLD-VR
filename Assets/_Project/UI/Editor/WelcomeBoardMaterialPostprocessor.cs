using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Wonderland.UI.Editor
{
    // Re-skins the embedded materials of medieval_notice_board.glb after every import,
    // because glTFast regenerates them and the standard Material Remapping path is unreliable
    // for this ScriptedImporter.
    //
    // Pages (Page_1, PH_2, PG_3..PG_6) and Poster_Back are intentionally left alone — those
    // are managed by the localized poster pipeline.
    public sealed class WelcomeBoardMaterialPostprocessor : AssetPostprocessor
    {
        private const string GlbAssetPath = "Assets/_Project/Art/Models/medieval_notice_board.glb";
        private const string TemplatesFolder = "Assets/_Project/UI/WelcomeBoard/Materials";
        private const string ToonShaderName = "Wonderland/UI/Notice Board Toon URP";

        // Embedded material name -> template asset name (without extension)
        private static readonly Dictionary<string, string> NameToTemplate = new Dictionary<string, string>
        {
            { "Main_Body",        "NB_MainBody_Mat" },
            { "Posts1",           "NB_Posts_Mat" },
            { "Bottom_bar",       "NB_BottomBar_Mat" },
            { "Roof_connection1", "NB_RoofConnection_Mat" },
            { "Roof_underneath",  "NB_RoofUnderneath_Mat" },
            { "Tiles1",           "NB_Tiles_Mat" },
            { "pins",             "NB_Pins_Mat" },
        };

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool glbTouched = false;
            foreach (string path in importedAssets)
            {
                if (path == GlbAssetPath)
                {
                    glbTouched = true;
                    break;
                }
            }
            if (!glbTouched)
            {
                return;
            }

            Shader toonShader = Shader.Find(ToonShaderName);
            if (toonShader == null)
            {
                Debug.LogWarning($"[WelcomeBoardMaterialPostprocessor] Shader '{ToonShaderName}' not found — skipping re-skin.");
                return;
            }

            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(GlbAssetPath);
            int rewritten = 0;
            int meshesBaked = 0;
            foreach (Object obj in subAssets)
            {
                if (obj is Material mat)
                {
                    if (!NameToTemplate.TryGetValue(mat.name, out string templateName))
                    {
                        continue;
                    }

                    Material template = AssetDatabase.LoadAssetAtPath<Material>($"{TemplatesFolder}/{templateName}.mat");
                    if (template == null)
                    {
                        Debug.LogWarning($"[WelcomeBoardMaterialPostprocessor] Missing template '{templateName}' — '{mat.name}' left unchanged.");
                        continue;
                    }

                    // glTFast can name the diffuse slot _BaseMap or baseColorTexture depending on shader.
                    Texture preservedBaseMap = TryGetTexture(mat, "_BaseMap", "_MainTex", "baseColorTexture");

                    mat.shader = toonShader;
                    mat.CopyPropertiesFromMaterial(template);

                    if (preservedBaseMap != null)
                    {
                        mat.SetTexture("_BaseMap", preservedBaseMap);
                    }

                    EditorUtility.SetDirty(mat);
                    rewritten++;
                }
                else if (obj is Mesh mesh)
                {
                    if (BakeSmoothedNormalsToUV1(mesh))
                    {
                        meshesBaked++;
                    }
                }
            }

            if (rewritten > 0 || meshesBaked > 0)
            {
                Debug.Log($"[WelcomeBoardMaterialPostprocessor] Re-skinned {rewritten} material(s) and baked smoothed normals on {meshesBaked} mesh(es).");
            }
        }

        // Averages NORMALs across all vertices sharing a position and packs the result into UV1.xyz
        // (encoded as (n+1)*0.5, with w = 1 as a "valid" sentinel). The outline pass uses this so
        // the inverted-hull-style push is continuous at hard-edge corners (cubes, prisms) where the
        // raw NORMAL stream is split per face and would otherwise crack.
        private static bool BakeSmoothedNormalsToUV1(Mesh mesh)
        {
            if (!mesh.isReadable)
            {
                Debug.LogWarning($"[WelcomeBoardMaterialPostprocessor] Mesh '{mesh.name}' is not readable; smoothed-normal bake skipped. Enable Read/Write on the GLB importer.");
                return false;
            }

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            if (vertices == null || normals == null || vertices.Length == 0 || normals.Length != vertices.Length)
            {
                return false;
            }

            var sumByPos = new Dictionary<Vector3, Vector3>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i];
                if (sumByPos.TryGetValue(p, out Vector3 acc))
                {
                    sumByPos[p] = acc + normals[i];
                }
                else
                {
                    sumByPos[p] = normals[i];
                }
            }

            var packed = new List<Vector4>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 n = sumByPos[vertices[i]];
                if (n.sqrMagnitude < 1e-10f)
                {
                    n = normals[i];
                }
                n = n.normalized;
                packed.Add(new Vector4((n.x + 1f) * 0.5f, (n.y + 1f) * 0.5f, (n.z + 1f) * 0.5f, 1f));
            }

            mesh.SetUVs(1, packed);
            EditorUtility.SetDirty(mesh);
            return true;
        }

        [MenuItem("Wonderful World/UI/Reapply Welcome Board Toon Shader")]
        public static void ReapplyWelcomeBoardToonShader()
        {
            AssetDatabase.ImportAsset(GlbAssetPath, ImportAssetOptions.ForceUpdate);
        }

        private static Texture TryGetTexture(Material mat, params string[] names)
        {
            foreach (string n in names)
            {
                if (mat.HasProperty(n))
                {
                    Texture t = mat.GetTexture(n);
                    if (t != null)
                    {
                        return t;
                    }
                }
            }
            return null;
        }
    }
}
