using PlantGeneration.Kelp;
using PlantGeneration.SpaceColonisation;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


namespace PlantGeneration {
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(GiantKelpGen))]
    [RequireComponent(typeof(SpaceColonization))]
    [RequireComponent(typeof(MeshRenderer))]
    public class GeneratorPreview : MonoBehaviour{
        public PlantGenSettings settings;
        [SerializeField] int seed = 0;
        public void RandomizeSeed(){
            seed = Random.Range(-1000, 1000);
        }
        public void Preview() {
            //float startTime = Time.realtimeSinceStartup;
            settings.GetGenerator().Initialize(settings);
            //Debug.Log("Initialization: " + ((Time.realtimeSinceStartup - startTime) * 1000).ToString() + "ms");
            GetComponent<MeshFilter>().sharedMesh = settings.GetGenerator().Generate(settings, seed);
            GetComponent<MeshRenderer>().sharedMaterial = settings.material;
            //Debug.Log("Together: " + ((Time.realtimeSinceStartup - startTime) * 1000).ToString() + "ms");
        }
        public void Clear() {
            DestroyImmediate(GetComponent<MeshFilter>().sharedMesh);
            GetComponent<MeshFilter>().sharedMesh = null;
        }

        public void Generate(){
            settings.GetGenerator().Initialize(settings);
            //GameObject newGO = new GameObject();
            //newGO.AddComponent<MeshFilter>().sharedMesh = settings.GetGenerator().Generate(settings, seed);
            //newGO.AddComponent<MeshRenderer>().sharedMaterial = settings.material;
            //newGO.transform.parent = transform;
            //newGO.name = settings.speciesName + " " + seed;
            //AssetDatabase.CreateAsset(newGO.GetComponent<MeshFilter>().sharedMesh, "Assets/Terrain/Generated/" + settings.speciesName + seed.ToString() + ".asset");
            //EditorUtility.SetDirty(newGO.GetComponent<MeshFilter>().sharedMesh);
            Mesh newMesh = settings.GetGenerator().Generate(settings, seed);
            AssetDatabase.CreateAsset(newMesh, "Assets/Terrain/Generated/" + settings.speciesName + seed.ToString() + ".asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}