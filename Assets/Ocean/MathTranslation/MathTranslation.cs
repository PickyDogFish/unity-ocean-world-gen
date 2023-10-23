using OceanSimulation;
using UnityEngine;

public class MathTranslation : MonoBehaviour
{
    [SerializeField] private Transform mainLight;
    [SerializeField] private int texSize = 128;
    [SerializeField] private RenderTexture heightTex;
    [SerializeField] private RenderTexture normalTex;
    [SerializeField] private RenderTexture displacementTex;
    [SerializeField] private ComputeShader mathShader;
    [SerializeField] private float len = 128;
    [SerializeField] private Vector2 wind = new Vector2(5,2);
    [SerializeField] private float phillipsA = 0.1f;
    private Texture2D gaussianNoise;
    
    [SerializeField] public Material material;
    void Start()
    {

        heightTex = new RenderTexture(texSize, texSize, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true
        };
        normalTex = new RenderTexture(texSize, texSize, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true
        };
        displacementTex = new RenderTexture(texSize, texSize, 0, RenderTextureFormat.RGFloat)
        {
            enableRandomWrite = true
        };
        
        gaussianNoise = GaussianNoise.GenerateTex(texSize);

    }

    // Update is called once per frame
    void Update()
    {
        SetMaterialVariables();
        SetCSVariables();
        mathShader.Dispatch(0, texSize/8, texSize/8, 1);
    }

    void SetCSVariables(){
        //all called in update so changes in shader update live
        mathShader.SetTexture(0, Shader.PropertyToID("_HeightTex"), heightTex);
        mathShader.SetTexture(0, Shader.PropertyToID("_NormalTex"), normalTex);
        mathShader.SetTexture(0, Shader.PropertyToID("_DisplacementTex"), displacementTex);
        mathShader.SetTexture(0, Shader.PropertyToID("_NoiseTex"), gaussianNoise);
        //mathShader.SetTexture(0, Shader.PropertyToID("_SpectrumTex"), spectrumGen.spectrumTexture);
        //mathShader.SetInt(Shader.PropertyToID("_NumOfSines"), numOfSines);
        mathShader.SetInt(Shader.PropertyToID("_N"), texSize);
        mathShader.SetFloat(Shader.PropertyToID("_Length"), len);
        mathShader.SetVector(Shader.PropertyToID("_Wind"), wind);
        mathShader.SetFloat(Shader.PropertyToID("_Time"), Time.fixedTime);
        mathShader.SetFloat(Shader.PropertyToID("_A"), phillipsA);
    }

    void SetMaterialVariables(){
        material.SetTexture("_HeightMap", heightTex);
        material.SetTexture("_NormalMap", normalTex);
        material.SetTexture("_DisplacementMap", displacementTex);
    }
}
