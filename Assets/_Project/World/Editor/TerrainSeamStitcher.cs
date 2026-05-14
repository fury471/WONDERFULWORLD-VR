using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TerrainSeamStitcher
{
    private const float PositionTolerance = 0.05f;

    [MenuItem("Wonderland/World/Stitch Active Scene Terrains")]
    public static void StitchActiveSceneTerrains()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains == null || terrains.Length == 0)
        {
            Debug.LogWarning("TerrainSeamStitcher: no active terrains found in the current scene.");
            return;
        }

        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            Terrain left = FindNeighbor(terrains, terrain, -1, 0);
            Terrain top = FindNeighbor(terrains, terrain, 0, 1);
            Terrain right = FindNeighbor(terrains, terrain, 1, 0);
            Terrain bottom = FindNeighbor(terrains, terrain, 0, -1);
            terrain.SetNeighbors(left, top, right, bottom);
        }

        HashSet<TerrainData> edited = new();
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            Terrain right = FindNeighbor(terrains, terrain, 1, 0);
            if (right != null)
            {
                RegisterUndoOnce(edited, terrain.terrainData);
                RegisterUndoOnce(edited, right.terrainData);
                StitchEastWest(terrain, right);
            }

            Terrain top = FindNeighbor(terrains, terrain, 0, 1);
            if (top != null)
            {
                RegisterUndoOnce(edited, terrain.terrainData);
                RegisterUndoOnce(edited, top.terrainData);
                StitchSouthNorth(terrain, top);
            }
        }

        foreach (TerrainData terrainData in edited)
        {
            EditorUtility.SetDirty(terrainData);
        }

        Debug.Log($"TerrainSeamStitcher: stitched {terrains.Length} active terrains and updated {edited.Count} TerrainData assets.");
    }

    private static Terrain FindNeighbor(IReadOnlyList<Terrain> terrains, Terrain source, int xDirection, int zDirection)
    {
        if (source == null || source.terrainData == null)
        {
            return null;
        }

        Vector3 sourcePosition = source.transform.position;
        Vector3 sourceSize = source.terrainData.size;
        Vector3 expected = sourcePosition + new Vector3(sourceSize.x * xDirection, 0f, sourceSize.z * zDirection);

        for (int i = 0; i < terrains.Count; i++)
        {
            Terrain candidate = terrains[i];
            if (candidate == null || candidate == source || candidate.terrainData == null)
            {
                continue;
            }

            Vector3 delta = candidate.transform.position - expected;
            if (Mathf.Abs(delta.x) <= PositionTolerance &&
                Mathf.Abs(delta.y) <= PositionTolerance &&
                Mathf.Abs(delta.z) <= PositionTolerance)
            {
                return candidate;
            }
        }

        return null;
    }

    private static void StitchEastWest(Terrain west, Terrain east)
    {
        TerrainData westData = west.terrainData;
        TerrainData eastData = east.terrainData;
        int westResolution = westData.heightmapResolution;
        int eastResolution = eastData.heightmapResolution;
        float[,] westEdge = new float[westResolution, 1];
        float[,] eastEdge = new float[eastResolution, 1];

        for (int z = 0; z < westResolution; z++)
        {
            float zNormalized = z / Mathf.Max(1f, westResolution - 1f);
            westEdge[z, 0] = WorldHeightToNormalized(west, AverageWorldHeight(west, 1f, zNormalized, east, 0f, zNormalized));
        }

        for (int z = 0; z < eastResolution; z++)
        {
            float zNormalized = z / Mathf.Max(1f, eastResolution - 1f);
            eastEdge[z, 0] = WorldHeightToNormalized(east, AverageWorldHeight(west, 1f, zNormalized, east, 0f, zNormalized));
        }

        westData.SetHeights(westResolution - 1, 0, westEdge);
        eastData.SetHeights(0, 0, eastEdge);
        west.Flush();
        east.Flush();
    }

    private static void StitchSouthNorth(Terrain south, Terrain north)
    {
        TerrainData southData = south.terrainData;
        TerrainData northData = north.terrainData;
        int southResolution = southData.heightmapResolution;
        int northResolution = northData.heightmapResolution;
        float[,] southEdge = new float[1, southResolution];
        float[,] northEdge = new float[1, northResolution];

        for (int x = 0; x < southResolution; x++)
        {
            float xNormalized = x / Mathf.Max(1f, southResolution - 1f);
            southEdge[0, x] = WorldHeightToNormalized(south, AverageWorldHeight(south, xNormalized, 1f, north, xNormalized, 0f));
        }

        for (int x = 0; x < northResolution; x++)
        {
            float xNormalized = x / Mathf.Max(1f, northResolution - 1f);
            northEdge[0, x] = WorldHeightToNormalized(north, AverageWorldHeight(south, xNormalized, 1f, north, xNormalized, 0f));
        }

        southData.SetHeights(0, southResolution - 1, southEdge);
        northData.SetHeights(0, 0, northEdge);
        south.Flush();
        north.Flush();
    }

    private static float AverageWorldHeight(Terrain a, float aX, float aZ, Terrain b, float bX, float bZ)
    {
        float aHeight = a.transform.position.y + a.terrainData.GetInterpolatedHeight(aX, aZ);
        float bHeight = b.transform.position.y + b.terrainData.GetInterpolatedHeight(bX, bZ);
        return (aHeight + bHeight) * 0.5f;
    }

    private static float WorldHeightToNormalized(Terrain terrain, float worldHeight)
    {
        return Mathf.Clamp01((worldHeight - terrain.transform.position.y) / Mathf.Max(0.0001f, terrain.terrainData.size.y));
    }

    private static void RegisterUndoOnce(ISet<TerrainData> edited, TerrainData terrainData)
    {
        if (terrainData != null && edited.Add(terrainData))
        {
            Undo.RegisterCompleteObjectUndo(terrainData, "Stitch Terrain Seams");
        }
    }
}
