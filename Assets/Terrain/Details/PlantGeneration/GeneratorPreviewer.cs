using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace PlantGeneration {
    [CustomEditor(typeof(GeneratorPreview))]
    public class GeneratorPreviewer : Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            GeneratorPreview plantPreviewer = (GeneratorPreview)target;
            if (GUILayout.Button("Preview")) {
                plantPreviewer.Preview();
            }
            if (GUILayout.Button("Clear Preview")) {
                plantPreviewer.Clear();
            }
            if (GUILayout.Button("Generate")) {
                plantPreviewer.Generate();
            }
        }
    }
}