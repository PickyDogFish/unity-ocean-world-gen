using OceanSimulation;
using UnityEngine;

public class MathTranslation : MonoBehaviour
{
    public enum SpectrumType{
        Phillips = 0,
        PiersonMoskowitz = 1
    }

    [Header("Initial spectrum settings")]
    [SerializeField] private ComputeShader spectrumShader;
    [SerializeField] private RenderTexture initialSpectrumTex;
    [SerializeField] private SpectrumType spectrumType;
    [SerializeField] private Vector2 wind = new Vector2(5,2);
    [SerializeField] private float phillipsA = 0.1f;
    [SerializeField] private bool updateSpectrum = false;
    
    
    
    [Header("Other settings")]
    [SerializeField] private int FFTSize = 128;
    [SerializeField] private float len = 128;
    [SerializeField] private float repeatTime = 200;
    [SerializeField] private ComputeShader mathShader;

    

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
        
        heightTex = new RenderTexture(FFTSize, FFTSize, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true
        };
        normalTex = new RenderTexture(FFTSize, FFTSize, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true
        };
        displacementTex = new RenderTexture(FFTSize, FFTSize, 0, RenderTextureFormat.RGFloat)
        {
            enableRandomWrite = true
        };
        
        gaussianNoise = GaussianNoise.GenerateTex(FFTSize);
        CalculateInitialSpectrum();
        CalculateConjugatedSpectrum();
    }

    void CalculateInitialSpectrum(){
        spectrumShader.SetTexture(0, Shader.PropertyToID("_NoiseTex"), gaussianNoise);
        spectrumShader.SetTexture(0, Shader.PropertyToID("_InitialSpectrumTex"), initialSpectrumTex);
        spectrumShader.SetFloat("_A", phillipsA);
        spectrumShader.SetFloat(Shader.PropertyToID("_Length"), len);
        spectrumShader.SetInt(Shader.PropertyToID("_Size"), FFTSize);
        spectrumShader.SetInt(Shader.PropertyToID("_SpectrumType"), (int) spectrumType);
        spectrumShader.SetVector(Shader.PropertyToID("_Wind"), wind);
        spectrumShader.Dispatch(0, FFTSize/8, FFTSize/8, 1);
    }

    void CalculateConjugatedSpectrum(){
        spectrumShader.SetTexture(1, Shader.PropertyToID("_InitialSpectrumTex"), initialSpectrumTex);
        spectrumShader.SetInt(Shader.PropertyToID("_Size"), FFTSize);
        spectrumShader.Dispatch(1, FFTSize/8, FFTSize/8, 1);
    }

    // Update is called once per frame
    void Update()
    {
        if (updateSpectrum){
            CalculateInitialSpectrum();
            CalculateConjugatedSpectrum();
        }
        SetMaterialVariables();
        SetCSVariables();
        mathShader.Dispatch(0, FFTSize/8, FFTSize/8, 1);
    }

    void SetCSVariables(){
        //all called in update so changes in shader update live
        mathShader.SetTexture(0, Shader.PropertyToID("_HeightTex"), heightTex);
        mathShader.SetTexture(0, Shader.PropertyToID("_NormalTex"), normalTex);
        mathShader.SetTexture(0, Shader.PropertyToID("_DisplacementTex"), displacementTex);
        mathShader.SetTexture(0, Shader.PropertyToID("_InitialSpectrumTex"), initialSpectrumTex);
        mathShader.SetInt(Shader.PropertyToID("_N"), FFTSize);
        mathShader.SetFloat(Shader.PropertyToID("_Length"), len);
        mathShader.SetFloat(Shader.PropertyToID("_Time"), Time.time);
        mathShader.SetFloat(Shader.PropertyToID("_RepeatTime"), repeatTime);
    }

    void SetMaterialVariables(){
        material.SetTexture("_HeightMap", heightTex);
        material.SetTexture("_NormalMap", normalTex);
        material.SetTexture("_DisplacementMap", displacementTex);
    }
}
