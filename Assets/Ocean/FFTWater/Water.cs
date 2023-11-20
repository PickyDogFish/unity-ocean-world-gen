using OceanSimulation;
using UnityEngine;

public class Water : MonoBehaviour
{
    public enum SpectrumType{
        Phillips = 0,
        PiersonMoskowitz = 1
    }

    [Header("Initial spectrum settings")]
    [SerializeField] private ComputeShader spectrumCS;
    [SerializeField] private RenderTexture initialSpectrumTex;
    [SerializeField] private RenderTexture timeSpectrumTex;
    [SerializeField] private SpectrumType spectrumType;
    [SerializeField] private float windAngle = 0;
    [SerializeField] private float windMagnitude = 0;
    [SerializeField] private float phillipsA = 0.1f;
    [SerializeField] private float lowCutoff = 0.0001f;
    [SerializeField] private float highCutoff = 10000.0f;
    [SerializeField] private bool updateSpectrum = false;
    [SerializeField] private float repeatTime = 200;
    [Range(0.1f, 2.0f)][SerializeField] private float speed = 1;
    
    
    
    [Header("Other settings")]
    [SerializeField] private int FFTSize = 128;
    [SerializeField] private float len = 128;
    [SerializeField] private ComputeShader fftWaterCS;
    [SerializeField] private float displacementMagnitude = 1;

    

    [Header("Previews")]
    [SerializeField] private RenderTexture heightTex;
    [SerializeField] private RenderTexture normalTex;
    [SerializeField] private RenderTexture displacementTex;
    private Texture2D gaussianNoise;
    
    [SerializeField] public Material material;
    void Start()
    {

        initialSpectrumTex = new RenderTexture(FFTSize, FFTSize, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true
        };
        timeSpectrumTex = new RenderTexture(FFTSize, FFTSize, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true
        };
        
        heightTex = new RenderTexture(FFTSize, FFTSize, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true
        };
        normalTex = new RenderTexture(FFTSize, FFTSize, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true
        };
        displacementTex = new RenderTexture(FFTSize, FFTSize, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true
        };
        
        gaussianNoise = GaussianNoise.GenerateTex(FFTSize);
        CalculateInitialSpectrum();
        CalculateConjugatedSpectrum();
    }

    void CalculateInitialSpectrum(){
        spectrumCS.SetTexture(0, Shader.PropertyToID("_NoiseTex"), gaussianNoise);
        spectrumCS.SetTexture(0, Shader.PropertyToID("_InitialSpectrumTex"), initialSpectrumTex);
        spectrumCS.SetFloat(Shader.PropertyToID("_A"), phillipsA);
        spectrumCS.SetFloat(Shader.PropertyToID("_LowCutoff"), lowCutoff);
        spectrumCS.SetFloat(Shader.PropertyToID("_HighCutoff"), highCutoff);
        spectrumCS.SetFloat(Shader.PropertyToID("_Length"), len);
        spectrumCS.SetInt(Shader.PropertyToID("_Size"), FFTSize);
        spectrumCS.SetInt(Shader.PropertyToID("_SpectrumType"), (int) spectrumType);
        spectrumCS.SetFloat(Shader.PropertyToID("_WindAngle"), windAngle);
        spectrumCS.SetFloat(Shader.PropertyToID("_WindMagnitude"), windMagnitude);
        spectrumCS.Dispatch(0, FFTSize/8, FFTSize/8, 1);
    }

    void CalculateConjugatedSpectrum(){
        spectrumCS.SetTexture(1, Shader.PropertyToID("_InitialSpectrumTex"), initialSpectrumTex);
        spectrumCS.SetInt(Shader.PropertyToID("_Size"), FFTSize);
        spectrumCS.Dispatch(1, FFTSize/8, FFTSize/8, 1);
    }

    void CalculateTimeSpectrum(){
        spectrumCS.SetTexture(2, Shader.PropertyToID("_InitialSpectrumTex"), initialSpectrumTex);
        spectrumCS.SetTexture(2, Shader.PropertyToID("_TimeSpectrumTex"), timeSpectrumTex);
        spectrumCS.SetFloat(Shader.PropertyToID("_Time"), Time.time * speed);
        spectrumCS.SetFloat(Shader.PropertyToID("_RepeatTime"), repeatTime);
        spectrumCS.Dispatch(2, FFTSize/8, FFTSize/8, 1);
    }

    // Update is called once per frame
    void Update()
    {
        if (updateSpectrum){
            CalculateInitialSpectrum();
            CalculateConjugatedSpectrum();
        }
        CalculateTimeSpectrum();
        SetMaterialVariables();
        SetCSVariables();
        fftWaterCS.Dispatch(0, FFTSize/8, FFTSize/8, 1);
    }

    void SetCSVariables(){
        //all called in update so changes in shader update live
        fftWaterCS.SetTexture(0, Shader.PropertyToID("_HeightTex"), heightTex);
        fftWaterCS.SetTexture(0, Shader.PropertyToID("_NormalTex"), normalTex);
        fftWaterCS.SetTexture(0, Shader.PropertyToID("_DisplacementTex"), displacementTex);
        fftWaterCS.SetTexture(0, Shader.PropertyToID("_TimeSpectrumTex"), timeSpectrumTex);
        fftWaterCS.SetInt(Shader.PropertyToID("_N"), FFTSize);
        fftWaterCS.SetFloat(Shader.PropertyToID("_Length"), len);
    }

    void SetMaterialVariables(){
        material.SetTexture("_HeightMap", heightTex);
        material.SetTexture("_NormalMap", normalTex);
        material.SetTexture("_DisplacementMap", displacementTex);
        material.SetFloat("_Displacement", displacementMagnitude);
    }
}