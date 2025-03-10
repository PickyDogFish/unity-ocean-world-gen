using UnityEngine;

namespace PlantGeneration.SpaceColonisation {

    [CreateAssetMenu(fileName = "New SC Settings", menuName = "Flora/SC Settings")]
    public class CoralSCSettings : PlantGenSettings {
        [Header("Coral settings")]
        public int maxIterations = 500;
        public float radius = 1;

        [Header("Attractor settings")]
        public int attractorCount = 100;
        public float killRange = 0.1f;
        public float attractionRange = 0.5f;
        public Vector3 attractorFieldOffset = Vector3.up;
        //public bool useNoise = false;
        //public float noiseScale = 1;


        [Header("Branch settings")]
        public float branchLength = 0.2f;
        public int branchRadialSubdivisions = 4;
        public float maxAngleDegrees = 90;
        [Tooltip("diameter should be less than branchLength")] public float branchBaseDiameter = 0.1f;

        public override PlantGenerator GetGenerator()
        {
            return FindObjectOfType<SpaceColonization>();
        }

    }
}