using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridBuilder))]
public class GridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GridBuilder terrainGenerator = (GridBuilder)target;
        if (GUILayout.Button("Generate grid")) {
            MeshFilter mesh = terrainGenerator.gameObject.GetComponent<MeshFilter>();
            mesh.sharedMesh = GridBuilder.BuildPlane(64,64, Vector3.zero);
        }
    }
}
