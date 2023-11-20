using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OceanRendererFeature : ScriptableRendererFeature
{
    public Shader waterShader;
    OceanPass m_WaterPass;
    OceanUnderwaterEffectPass m_UnderwaterPass;
    private Material material;

    public override void Create()
    {
        name = "Ocean";
        // Configures where the render pass should be injected.
        m_WaterPass = new OceanPass(material);
        m_UnderwaterPass = new OceanUnderwaterEffectPass();
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game || renderingData.cameraData.cameraType == CameraType.SceneView){
            material = CoreUtils.CreateEngineMaterial(waterShader);
            m_WaterPass.ConfigureInput(ScriptableRenderPassInput.Color);
            m_WaterPass.SetTarget(renderer.cameraColorTarget);
            renderer.EnqueuePass(m_WaterPass);

            //renderer.EnqueuePass(m_UnderwaterPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(material);
    }
}


