using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MarchingCubes;

namespace PlantGeneration.ReactionDiffusion
{
    public class RDLayered : PlantGenerator
    {
        ComputeBuffer _voxelBuffer;
        MeshBuilder _builder;
        [SerializeField] ComputeShader _builderCompute = null;
        [SerializeField] ComputeShader simulationCompute = null;



        private RenderTexture simulationRead;
        private RenderTexture simulationWrite;

        float[] values;


        public override void Initialize(PlantGenSettings settings){
            //float startTime = Time.realtimeSinceStartup;
            RDLayerSettings layerSettings = (RDLayerSettings) settings;
            values = new float[layerSettings.size * layerSettings.size * layerSettings.size];
            _voxelBuffer = new ComputeBuffer(layerSettings.size * layerSettings.size * layerSettings.size, sizeof(float));
            _builder = new MeshBuilder(new Vector3Int(layerSettings.size, layerSettings.size, layerSettings.size), layerSettings.builderTriangleBudget, _builderCompute);
            simulationRead = RDSimulator.CreateRenderTexture(layerSettings.simulationSettings.resolution);
            simulationWrite = RDSimulator.CreateRenderTexture(layerSettings.simulationSettings.resolution);
            //Debug.Log("Initialization: " + ((Time.realtimeSinceStartup - startTime) * 1000).ToString() + "ms");
        }

        public override Mesh Generate(PlantGenSettings settings, int seed){
            //float startTime = Time.realtimeSinceStartup;
            RDLayerSettings layerSettings = (RDLayerSettings)settings;
            RDSimulator.InitializeComputeShader(ref simulationCompute, layerSettings.simulationSettings, ref simulationRead);

            //building the values array from layers from RDOnGPU
            for (int layerIndex = 0; layerIndex < layerSettings.size; layerIndex++)
            {
                AddNextLayerToValues(layerSettings, layerIndex);
            }
            //float simTimeDelta = Time.realtimeSinceStartup - startTime;
            //Debug.Log("Simulation: " + (simTimeDelta * 1000).ToString() + "ms");
            _voxelBuffer.SetData(values);
            _builder.BuildIsosurface(_voxelBuffer, layerSettings.builderTargetValue, layerSettings.builderGridScale);
            //Debug.Log("Finished generating coral");
            Mesh mesh = _builder.Mesh;
            //mesh.RecalculateBounds();
            _builder.Dispose();
            //float triTime = Time.realtimeSinceStartup - startTime - simTimeDelta;
            //Debug.Log("Triangulation: " + (triTime * 1000).ToString() + "ms");
            return mesh;
        }

        void AddNextLayerToValues(RDLayerSettings layerSettings, int layerIndex)
        {
            float extraKill = Mathf.Clamp01(layerSettings.killIncrease.Evaluate((float)layerIndex/layerSettings.size));
            RDSimulator.Iterate(ref simulationRead, ref simulationWrite, ref simulationCompute, layerSettings.simulationSettings, extraKill, layerIndex, layerSettings.step);
            Texture2D tex = RandomUtils.ToTexture2D(simulationWrite, layerSettings.simulationSettings.resolution, 1);
            //Saving the layers to png files, only here so you can view the individual layers.
            //if (layerIndex % 5 == 0 && layerIndex < 80){
            //    RandomUtils.SaveTexture(RandomUtils.ToBW(tex, 0), "layer-" + layerIndex + ".png");
            //}
            TextureScaler.Scale(tex, layerSettings.size, layerSettings.size);

            Color[] colors = tex.GetPixels();
            for (int i = 0; i < layerSettings.size * layerSettings.size; i++)
            {
                values[layerIndex * layerSettings.size * layerSettings.size + i] = colors[i].g;
            }
        }

        void OnDestroy()
        {
            CleanUp();
        }

        public void CleanUp(){
            if (_voxelBuffer != null) _voxelBuffer.Dispose();
            if (_builder != null) _builder.Dispose();
        }


    }
}