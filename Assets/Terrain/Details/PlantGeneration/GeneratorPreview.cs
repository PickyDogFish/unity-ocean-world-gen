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
    public class GeneratorPreview : MonoBehaviour {
        public PlantGenSettings settings;
        [SerializeField] int seed = 0;
        public void RandomizeSeed(){
            seed = Random.Range(-1000, 1000);
        }
        public void Preview() {
            settings.GetGenerator().Initialize(settings);
            GetComponent<MeshFilter>().sharedMesh = settings.GetGenerator().Generate(settings, seed);
            GetComponent<MeshRenderer>().sharedMaterial = settings.material;
        }
        public void Clear() {
            DestroyImmediate(GetComponent<MeshFilter>().sharedMesh);
            GetComponent<MeshFilter>().sharedMesh = null;
        }

        public void Generate(){
            settings.GetGenerator().Initialize(settings);
            GameObject newGO = new GameObject();
            newGO.AddComponent<MeshFilter>().sharedMesh = settings.GetGenerator().Generate(settings, seed);
            newGO.AddComponent<MeshRenderer>().sharedMaterial = settings.material;
            newGO.transform.parent = transform;
            newGO.name = settings.speciesName + " " + seed;
        }
    }
}