using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OceanRendererFeature : ScriptableRendererFeature
{
    public Shader waterShader;
    OceanPass m_WaterPass;
    OceanUnderwaterEffectPass m_UnderwaterPass;
    OceanSunshaftsPass m_SunShaftsPass;
    [SerializeField] private bool renderUnderwater = true;
    [SerializeField] private bool renderSunShafts = true;

    public override void Create()
    {
        name = "Ocean";
        // Configures where the render pass should be injected.
        m_WaterPass = new OceanPass();
        m_UnderwaterPass = new OceanUnderwaterEffectPass();
        m_SunShaftsPass = new OceanSunshaftsPass();
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game || renderingData.cameraData.cameraType == CameraType.SceneView){
            if (renderUnderwater){
                m_UnderwaterPass.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_UnderwaterPass);
            }
            
            if (renderSunShafts){
                m_SunShaftsPass.ConfigureInput(ScriptableRenderPassInput.Depth);
                renderer.EnqueuePass(m_SunShaftsPass);
            }

            m_WaterPass.ConfigureInput(ScriptableRenderPassInput.Color);
            renderer.EnqueuePass(m_WaterPass);


        }
    }



    protected override void Dispose(bool disposing)
    {
    }
}


