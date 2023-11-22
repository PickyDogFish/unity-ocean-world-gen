using OceanSimulation;
using UnityEngine;

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
    [SerializeField] private float repeatTime = 200;
    [Range(0.1f, 2.0f)][SerializeField] private float speed = 1;
    private Texture2D gaussianNoise;
    int threadGroupsX;
    int threadGroupsY;
    [SerializeField] private float lengthScale = 8;
    //we divide world position by this number to get worldUVs
    [SerializeField] private float waveScale = 100;

    [SerializeField] private Material material;

    public RenderTexture heightTex,
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
    [SerializeField] private bool updateOcean = true;

    void Start()
    {
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
        if (updateSpectrum)
        {
            CalculateInitialSpectrum();
            CalculateConjugatedSpectrum();
        }

        if (updateOcean)
        {
            FFTCS.SetVector("_Lambda", displacementStrength);
            FFTCS.SetFloat("_NormalStrength", normalStrength);
        }

        SetMaterialVariables();
        
        CalculateTimeSpectrum();

        InverseFFT(htildeTex);
        InverseFFT(htildeSlopeXTex);
        InverseFFT(htildeSlopeZTex);
        InverseFFT(htildeDisplacementXTex);
        InverseFFT(htildeDisplacementZTex);

        AssembleMaps();
    }

    void AssembleMaps()
    {
        FFTCS.SetTexture(CSKernels.assembleMapsKernel, "_HTildeTex", htildeTex);
        FFTCS.SetTexture(CSKernels.assembleMapsKernel, "_HTildeSlopeXTex", htildeSlopeXTex);
        FFTCS.SetTexture(CSKernels.assembleMapsKernel, "_HTildeSlopeZTex", htildeSlopeZTex);
        FFTCS.SetTexture(CSKernels.assembleMapsKernel, "_HTildeDisplacementXTex", htildeDisplacementXTex);
        FFTCS.SetTexture(CSKernels.assembleMapsKernel, "_HTildeDisplacementZTex", htildeDisplacementZTex);
        FFTCS.SetTexture(CSKernels.assembleMapsKernel, "_HeightTex", heightTex);
        FFTCS.SetTexture(CSKernels.assembleMapsKernel, "_NormalTex", normalTex);
        FFTCS.Dispatch(CSKernels.assembleMapsKernel, threadGroupsX, threadGroupsY, 1);
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

    void CalculateTimeSpectrum()
    {
        spectrumCS.SetTexture(CSKernels.FFTTimeKernel, "_InitialSpectrumTex", initialSpectrumTex);
        spectrumCS.SetTexture(CSKernels.FFTTimeKernel, "_HTildeTex", htildeTex);
        spectrumCS.SetTexture(CSKernels.FFTTimeKernel, "_HTildeSlopeXTex", htildeSlopeXTex);
        spectrumCS.SetTexture(CSKernels.FFTTimeKernel, "_HTildeSlopeZTex", htildeSlopeZTex);
        spectrumCS.SetTexture(CSKernels.FFTTimeKernel, "_HTildeDisplacementXTex", htildeDisplacementXTex);
        spectrumCS.SetTexture(CSKernels.FFTTimeKernel, "_HTildeDisplacementZTex", htildeDisplacementZTex);
        spectrumCS.SetFloat("_Time", Time.time * speed);
        spectrumCS.SetFloat("_RepeatTime", repeatTime);
        spectrumCS.Dispatch(CSKernels.FFTTimeKernel, threadGroupsX, threadGroupsY, 1);
    }

    void InverseFFT(RenderTexture spectrumTex)
    {
        bool pingPong = false;
        FFTCS.SetTexture(CSKernels.verticalIFFTKernel, "_TwiddleTexture", twiddleTex);
        FFTCS.SetTexture(CSKernels.verticalIFFTKernel, "_Buffer0", spectrumTex);
        FFTCS.SetTexture(CSKernels.verticalIFFTKernel, "_Buffer1", pingPongTex);
        for (int i = 0; i < logN; ++i)
        {
            pingPong = !pingPong;
            FFTCS.SetInt("_Step", i);
            FFTCS.SetBool("_PingPong", pingPong);
            FFTCS.Dispatch(CSKernels.verticalIFFTKernel, threadGroupsX, threadGroupsY, 1);
        }

        FFTCS.SetTexture(CSKernels.horizontalIFFTKernel, "_TwiddleTexture", twiddleTex);
        FFTCS.SetTexture(CSKernels.horizontalIFFTKernel, "_Buffer0", spectrumTex);
        FFTCS.SetTexture(CSKernels.horizontalIFFTKernel, "_Buffer1", pingPongTex);
        for (int i = 0; i < logN; ++i)
        {
            pingPong = !pingPong;
            FFTCS.SetInt("_Step", i);
            FFTCS.SetBool("_PingPong", pingPong);
            FFTCS.Dispatch(CSKernels.horizontalIFFTKernel, threadGroupsX, threadGroupsY, 1);
        }

        if (pingPong) Graphics.Blit(pingPongTex, spectrumTex);

        FFTCS.SetTexture(CSKernels.permuteKernel, "_Buffer0", spectrumTex);
        FFTCS.Dispatch(CSKernels.permuteKernel, threadGroupsX, threadGroupsY, 1);
    }

    void SetMaterialVariables()
    {
        Shader.SetGlobalTexture("_OceanDisplacementTex", heightTex);
        Shader.SetGlobalFloat("Ocean_WaveScale", waveScale);
        //material.SetTexture("_OceanDisplacementTex", heightTex);
        material.SetTexture("_NormalMap", normalTex);
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
