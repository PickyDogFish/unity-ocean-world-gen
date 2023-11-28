using UnityEngine;
using UnityEngine.Rendering;

public class FFTWater : MonoBehaviour
{
    public enum SpectrumType
    {
        Phillips = 0,
        PiersonMoskowitz = 1
    }

    [Header("Initial spectrum settings")]
    [SerializeField] private ComputeShader spectrumCS;
    private RenderTexture initialSpectrumTex;
    [SerializeField] private SpectrumType spectrumType;
    [SerializeField] private float windAngle = 0;
    [SerializeField] private float windMagnitude = 8;
    [SerializeField] private float phillipsA = 0.1f;
    [SerializeField] private float lowCutoff = 0.0001f;
    [SerializeField] private float highCutoff = 10000.0f;
    [SerializeField] private bool updateSpectrum = false;
    
    [Header("Time spectrum settings")]
    [SerializeField] private float repeatTime = 200;
    [Range(0.1f, 2.0f)][SerializeField] private float speed = 1;
    private Texture2D gaussianNoise;
    int threadGroupsX;
    int threadGroupsY;
    [SerializeField] private float lengthScale = 8;
    //we divide world position by this number to get worldUVs
    [SerializeField] private float waveScale = 100;

    [SerializeField] private Material material;

    private RenderTexture heightTex,
                      normalTex,
                      twiddleTex,
                      pingPongTex,
                      htildeTex,
                      htildeSlopeXTex,
                      htildeSlopeZTex,
                      htildeDisplacementXTex,
                      htildeDisplacementZTex;




    int logN;

    [Header("FFT Settings")]
    [SerializeField] private ComputeShader FFTCS;
    public int FFTSize = 128;
    [SerializeField] private Vector2 displacementStrength = Vector2.one;
    [SerializeField] private float normalStrength = 1;


    [Header("Material settings")]
    [SerializeField] private Color fogColor = Color.black;
    [Range(0,1)][SerializeField] private float fogIntensity = 0.01f;
    [Range(0,0.1f)][SerializeField] private float refractionIntensity = 0.001f;

    [SerializeField] Transform playerTransform;

    [Header("Clipmap settings")] 
    [SerializeField] private int clipMapLevels = 4;
    [SerializeField] private int clipMapVertexDensity = 16;

    void Start()
    {
        //setting mesh bounds so it doesnt get culled when camera moves out of original mesh bounds
        GetComponentInChildren<MeshFilter>().mesh = GridBuilder.BuildClipMap(clipMapVertexDensity, clipMapLevels);


        threadGroupsX = FFTSize / 8;
        threadGroupsY = FFTSize / 8;
        logN = (int)Mathf.Log(FFTSize, 2);
        InitializeRenderTextures();


        gaussianNoise = GaussianNoise.GenerateTex(FFTSize);
        CalculateInitialSpectrum();
        CalculateConjugatedSpectrum();

        //TODO expose these two variables in the editor
        FFTCS.SetVector("_Lambda", displacementStrength);
        FFTCS.SetFloat("_NormalStrength", 1);

        FFTCS.SetTexture(CSKernels.twiddlePrecomputeKernel, "_TwiddleTexture", twiddleTex);
        FFTCS.SetInt("_Size", FFTSize);
        FFTCS.Dispatch(CSKernels.twiddlePrecomputeKernel, logN, FFTSize / 2 / 8, 1);

        SetMaterialVariables();

    }

    void Update()
    {
        GetComponent<MeshFilter>().mesh.bounds = new Bounds(playerTransform.position,  GetComponent<MeshFilter>().mesh.bounds.size);
        CommandBuffer cmd = CommandBufferPool.Get("OceanSimulation");

        if (updateSpectrum)
        {
            CalculateInitialSpectrum();
            CalculateConjugatedSpectrum();
        }

        SetMaterialVariables();
        
        CalculateTimeSpectrum(cmd);

        InverseFFT(cmd, htildeTex);
        InverseFFT(cmd, htildeSlopeXTex);
        InverseFFT(cmd, htildeSlopeZTex);
        InverseFFT(cmd, htildeDisplacementXTex);
        InverseFFT(cmd, htildeDisplacementZTex);
        AssembleMaps(cmd);
        Graphics.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }


    void AssembleMaps(CommandBuffer cmd)
    {
        cmd.SetComputeTextureParam(FFTCS, CSKernels.assembleMapsKernel, "_HTildeTex", htildeTex);
        cmd.SetComputeTextureParam(FFTCS, CSKernels.assembleMapsKernel, "_HTildeSlopeXTex", htildeSlopeXTex);
        cmd.SetComputeTextureParam(FFTCS, CSKernels.assembleMapsKernel, "_HTildeSlopeZTex", htildeSlopeZTex);
        cmd.SetComputeTextureParam(FFTCS, CSKernels.assembleMapsKernel, "_HTildeDisplacementXTex", htildeDisplacementXTex);
        cmd.SetComputeTextureParam(FFTCS, CSKernels.assembleMapsKernel, "_HTildeDisplacementZTex", htildeDisplacementZTex);
        cmd.SetComputeTextureParam(FFTCS, CSKernels.assembleMapsKernel, "_HeightTex", heightTex);
        cmd.SetComputeTextureParam(FFTCS, CSKernels.assembleMapsKernel, "_NormalTex", normalTex);
        cmd.SetComputeFloatParam(FFTCS, "_NormalStrength", normalStrength);
        cmd.SetComputeVectorParam(FFTCS, "_Lambda", displacementStrength);
        cmd.DispatchCompute(FFTCS, CSKernels.assembleMapsKernel, threadGroupsX, threadGroupsY, 1);
    }

    void InitializeRenderTextures()
    {
        initialSpectrumTex = CreateRenderTex(FFTSize, FFTSize, RenderTextureFormat.ARGBHalf);
        htildeTex = CreateRenderTex(FFTSize, FFTSize, RenderTextureFormat.RGHalf);
        htildeSlopeXTex = CreateRenderTex(FFTSize, FFTSize, RenderTextureFormat.RGHalf);
        htildeSlopeZTex = CreateRenderTex(FFTSize, FFTSize, RenderTextureFormat.RGHalf);
        htildeDisplacementXTex = CreateRenderTex(FFTSize, FFTSize, RenderTextureFormat.RGHalf);
        htildeDisplacementZTex = CreateRenderTex(FFTSize, FFTSize, RenderTextureFormat.RGHalf);
        pingPongTex = CreateRenderTex(FFTSize, FFTSize, RenderTextureFormat.RGHalf);
        heightTex = CreateRenderTex(FFTSize, FFTSize, RenderTextureFormat.ARGBHalf);
        heightTex.wrapMode = TextureWrapMode.Repeat;
        normalTex = CreateRenderTex(FFTSize, FFTSize, RenderTextureFormat.ARGBHalf);
        normalTex.wrapMode = TextureWrapMode.Repeat;
        twiddleTex = CreateRenderTex(logN, FFTSize, RenderTextureFormat.ARGBHalf);
    }

    void CalculateInitialSpectrum()
    {
        spectrumCS.SetTexture(CSKernels.initialSpectrumKernel, "_NoiseTex", gaussianNoise);
        spectrumCS.SetTexture(CSKernels.initialSpectrumKernel, "_InitialSpectrumTex", initialSpectrumTex);
        spectrumCS.SetFloat("_A", phillipsA);
        spectrumCS.SetFloat("_LowCutoff", lowCutoff);
        spectrumCS.SetFloat("_HighCutoff", highCutoff);
        spectrumCS.SetFloat("_LengthScale", lengthScale);
        spectrumCS.SetInt("_Size", FFTSize);
        spectrumCS.SetInt("_SpectrumType", (int)spectrumType);
        spectrumCS.SetFloat("_WindAngle", windAngle);
        spectrumCS.SetFloat("_WindMagnitude", windMagnitude);
        spectrumCS.Dispatch(CSKernels.initialSpectrumKernel, threadGroupsX, threadGroupsY, 1);
    }

    void CalculateConjugatedSpectrum()
    {
        spectrumCS.SetTexture(CSKernels.conjugateKernel, "_InitialSpectrumTex", initialSpectrumTex);
        spectrumCS.SetInt("_Size", FFTSize);
        spectrumCS.Dispatch(CSKernels.conjugateKernel, threadGroupsX, threadGroupsY, 1);
    }

    void CalculateTimeSpectrum(CommandBuffer cmd)
    {
        cmd.SetComputeTextureParam(spectrumCS, CSKernels.FFTTimeKernel, "_InitialSpectrumTex", initialSpectrumTex);
        cmd.SetComputeTextureParam(spectrumCS, CSKernels.FFTTimeKernel, "_HTildeTex", htildeTex);
        cmd.SetComputeTextureParam(spectrumCS, CSKernels.FFTTimeKernel, "_HTildeSlopeXTex", htildeSlopeXTex);
        cmd.SetComputeTextureParam(spectrumCS, CSKernels.FFTTimeKernel, "_HTildeSlopeZTex", htildeSlopeZTex);
        cmd.SetComputeTextureParam(spectrumCS, CSKernels.FFTTimeKernel, "_HTildeDisplacementXTex", htildeDisplacementXTex);
        cmd.SetComputeTextureParam(spectrumCS, CSKernels.FFTTimeKernel, "_HTildeDisplacementZTex", htildeDisplacementZTex);
        cmd.SetComputeFloatParam(spectrumCS, "_Time", Time.time * speed);
        cmd.SetComputeFloatParam(spectrumCS, "_RepeatTime", repeatTime);
        cmd.DispatchCompute(spectrumCS, CSKernels.FFTTimeKernel, threadGroupsX, threadGroupsY, 1);
    }

    void InverseFFT(CommandBuffer cmd, RenderTexture spectrumTex)
    {
        cmd.SetComputeTextureParam(FFTCS, CSKernels.verticalIFFTKernel, "_TwiddleTexture", twiddleTex);
        cmd.SetComputeTextureParam(FFTCS, CSKernels.verticalIFFTKernel, "_Buffer0", spectrumTex);
        cmd.SetComputeTextureParam(FFTCS, CSKernels.verticalIFFTKernel, "_Buffer1", pingPongTex);
        bool pingPong = false;
        for (int i = 0; i < logN; ++i)
        {
            pingPong = !pingPong;
            cmd.SetComputeIntParam(FFTCS, "_Step", i);
            cmd.SetComputeIntParam(FFTCS,"_PingPong", pingPong?1:0);
            cmd.DispatchCompute(FFTCS, CSKernels.verticalIFFTKernel, threadGroupsX, threadGroupsY, 1);
        }
        cmd.SetComputeTextureParam(FFTCS, CSKernels.horizontalIFFTKernel, "_TwiddleTexture", twiddleTex);
        cmd.SetComputeTextureParam(FFTCS, CSKernels.horizontalIFFTKernel, "_Buffer0", spectrumTex);
        cmd.SetComputeTextureParam(FFTCS, CSKernels.horizontalIFFTKernel, "_Buffer1", pingPongTex);
        for (int i = 0; i < logN; ++i)
        {
            pingPong = !pingPong;
            cmd.SetComputeIntParam(FFTCS, "_Step", i);
            cmd.SetComputeIntParam(FFTCS,"_PingPong", pingPong?1:0);
            cmd.DispatchCompute(FFTCS, CSKernels.horizontalIFFTKernel, threadGroupsX, threadGroupsY, 1);
        }

        if (pingPong) cmd.Blit(pingPongTex, spectrumTex);

        cmd.SetComputeTextureParam(FFTCS, CSKernels.permuteKernel, "_Buffer0", spectrumTex);
        cmd.DispatchCompute(FFTCS, CSKernels.permuteKernel, threadGroupsX, threadGroupsY, 1);
    }

    void SetMaterialVariables()
    {
        Shader.SetGlobalTexture("_OceanDisplacementTex", heightTex);
        Shader.SetGlobalFloat("Ocean_WaveScale", waveScale);
        Shader.SetGlobalVector("Ocean_FogColor", fogColor);
        Shader.SetGlobalFloat("Ocean_FogIntensity", fogIntensity);
        Shader.SetGlobalFloat("Ocean_RefractionIntensity", refractionIntensity);
        Shader.SetGlobalTexture("Ocean_CubeMap", ReflectionProbe.defaultTexture);
        //material.SetTexture("_OceanDisplacementTex", heightTex);
        Shader.SetGlobalTexture("_OceanNormalTex", normalTex);
        material.SetVector("ClipMap_ViewerPosition", playerTransform.position);
        //material.SetTexture("_DisplacementMap", displacementTex);
        //material.SetFloat("_Displacement", displacementMagnitude);
    }

    RenderTexture CreateRenderTex(int width, int height, RenderTextureFormat format)
    {
        RenderTexture rt = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear);
        rt.enableRandomWrite = true;
        rt.Create();

        return rt;
    }

    private static class CSKernels
    {
        public static readonly int twiddlePrecomputeKernel = 0;
        public static readonly int horizontalIFFTKernel = 1;
        public static readonly int verticalIFFTKernel = 2;
        public static readonly int permuteKernel = 3;
        public static readonly int assembleMapsKernel = 4;



        public static readonly int initialSpectrumKernel = 0;
        public static readonly int conjugateKernel = 1;
        public static readonly int DFTTimeKernel = 2;
        public static readonly int FFTTimeKernel = 3;
    }

}
