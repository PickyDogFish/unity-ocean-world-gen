using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGen))]
public class TerrainGenEditor : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        TerrainGen terrainGenerator = (TerrainGen)target;
        if (GUILayout.Button("Generate Terrain")) {
            terrainGenerator.InitializeTerrainGen();
            terrainGenerator.RemoveAllTerrain();
            terrainGenerator.GenerateAndShowNearbyTerrain();
        }
        if (GUILayout.Button("Clear Terrain")) {
            terrainGenerator.RemoveAllTerrain();
        }
    }
}
