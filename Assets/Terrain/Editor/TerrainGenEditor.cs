using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(TerrainGen))]
public class TerrainGenEditor : Editor
{
    RenderTexture noisePreview;
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        TerrainGen terrainGenerator = (TerrainGen)target;
        if (noisePreview){
            //EditorGUI.DrawPreviewTexture(new Rect(20, 300, 256, 256), noisePreview);
            EditorGUILayout.ObjectField("Noise preview", noisePreview, typeof(RenderTexture), allowSceneObjects: false);
        }
        if (GUILayout.Button("Generate Terrain")) {
            terrainGenerator.InitializeTerrainGen();
            terrainGenerator.RemoveAllTerrain();
            terrainGenerator.GenerateAndShowNearbyTerrain();
        }

        if (GUILayout.Button("Clear Terrain")) {
            terrainGenerator.RemoveAllTerrain();
        }
        if (GUILayout.Button("Show Noise Preview")){
            terrainGenerator.InitializeTerrainGen();
            noisePreview = terrainGenerator.PreviewNoise();
            Texture2D tex = RandomUtils.ToTexture2D(noisePreview);
            tex = RandomUtils.ToBW(tex);
            RandomUtils.SaveTexture(tex, "terrainHeight.png");
        }
        if (GUILayout.Button("Hide Noise Preview")){
            noisePreview = null;
        }

    }
}
