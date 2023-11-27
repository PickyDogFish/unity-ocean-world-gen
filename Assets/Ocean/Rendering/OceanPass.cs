using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OceanPass : ScriptableRenderPass
    {

        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Ocean");
        private ShaderTagId OceanShaderTagId = new ShaderTagId("OceanMain");
        private FilteringSettings _filteringSettings;


        public OceanPass(){
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
            cmd.Clear();
            CommandBufferPool.Release(cmd);

            context.ExecuteCommandBuffer(cmd);
            context.Submit();

        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

 public class OceanUnderwaterEffectPass : ScriptableRenderPass
    {
        //private readonly OceanRendererFeature.OceanRenderingSettings _settings;
        private readonly Material _underwaterEffectMaterial;
        private RenderTargetIdentifier _submergenceTarget;
        private static readonly int _submergenceTargetID = Shader.PropertyToID("SubmergenceTarget");
        public static readonly int SubmergenceTexture = Shader.PropertyToID("Ocean_CameraSubmergenceTexture");
        

        public OceanUnderwaterEffectPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            _underwaterEffectMaterial = new Material(Shader.Find("Ocean/UnderwaterEffect"));
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(_submergenceTargetID, 32, 32, 0, FilterMode.Bilinear, RenderTextureFormat.R8, RenderTextureReadWrite.Linear, 1);
            _submergenceTarget = new RenderTargetIdentifier(_submergenceTargetID);
            ConfigureTarget(_submergenceTarget);
        }

        private void DrawProceduralFullscreenQuad(CommandBuffer cmd, RenderTargetIdentifier target,
            RenderBufferLoadAction loadAction, Material material, int pass)
        {
            cmd.SetRenderTarget(target, loadAction, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Quads, 4, 1, null);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CameraData cameraData = renderingData.cameraData;
            //if (!OceanRendererFeature.IsCorrectCameraType(cameraData.cameraType)) return;

            //Drawing the fullscreen quad
            CommandBuffer cmd = CommandBufferPool.Get("Underwater Effect");

            SetupCameraGlobals(cmd, cameraData.camera);

            DrawProceduralFullscreenQuad(cmd, _submergenceTarget,
                RenderBufferLoadAction.DontCare, _underwaterEffectMaterial, 0);
            cmd.SetGlobalTexture(SubmergenceTexture, _submergenceTargetID);

            DrawProceduralFullscreenQuad(cmd, cameraData.renderer.cameraColorTarget,
                RenderBufferLoadAction.Load, _underwaterEffectMaterial, 1);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
            context.Submit();
        }

        private void SetupCameraGlobals(CommandBuffer cmd, Camera cam)
        {
            cmd.SetGlobalMatrix(Shader.PropertyToID("Ocean_InverseProjectionMatrix"),
                GL.GetGPUProjectionMatrix(cam.projectionMatrix, false).inverse);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_submergenceTargetID);
        }
    }
