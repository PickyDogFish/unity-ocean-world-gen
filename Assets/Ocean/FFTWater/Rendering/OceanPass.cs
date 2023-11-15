using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OceanPass : ScriptableRenderPass
    {

        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Ocean");
        private Material _waterMaterial;
        private RenderTargetHandle tempTexture;
        private ShaderTagId OceanShaderTagId = new ShaderTagId("OceanMain");
        private FilteringSettings _filteringSettings;

        RenderTargetIdentifier m_CameraColorTarget;

        public OceanPass(Material mat){
            _waterMaterial = mat;
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            _filteringSettings = new FilteringSettings(RenderQueueRange.all);
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        public void SetTarget(RenderTargetIdentifier colorHandle)
    {
        m_CameraColorTarget = colorHandle;
    }


        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("OceanRendererFeature");
            DrawingSettings drawingSettings = new DrawingSettings(OceanShaderTagId,
                new SortingSettings(renderingData.cameraData.camera));

            using (new ProfilingScope(cmd, m_ProfilingSampler)){
                drawingSettings.perObjectData = PerObjectData.LightProbe;
                //Drawing shaders with tag "OceanMain", as defined in drawingSettings
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);
            }
            //Blit(cmd, source, tempTexture.Identifier());

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);

        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempTexture.id);
        }
    }