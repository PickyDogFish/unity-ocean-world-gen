using OceanSimulation;
using UnityEngine;

public class SumOfSines : MonoBehaviour
{
    [SerializeField] private Transform mainLight;
    [SerializeField] private int texSize = 256, numOfSines = 4;
    [SerializeField] private float heightMult = 1;
    [SerializeField] private RenderTexture heightTex;
    [SerializeField] private RenderTexture normalTex;

    private SpectrumGenerator spectrumGen;
    [SerializeField] public Material material;
    [SerializeField] private ComputeShader sosShader;
    void Start()
    {
        spectrumGen = GetComponent<SpectrumGenerator>();

        heightTex = new RenderTexture(256, 256, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true
        };
        normalTex = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true
        };
        
        material.SetTexture("_HeightMap", heightTex);
        material.SetTexture("_NormalMap", normalTex);

    }

    // Update is called once per frame
    void Update()
    {
        SetMaterialVariables();
        SetCSVariables();
        material.SetFloat("_HeightMult", heightMult);
        sosShader.Dispatch(0, texSize/8, texSize/8, 1);
    }

    void SetCSVariables(){
        //all called in update so changes in shader update live
        sosShader.SetTexture(0, Shader.PropertyToID("_HeightTex"), heightTex);
        sosShader.SetTexture(0, Shader.PropertyToID("_NormalTex"), normalTex);
        sosShader.SetInt(Shader.PropertyToID("_TexSize"), texSize);
        sosShader.SetTexture(0, Shader.PropertyToID("_SpectrumTex"), spectrumGen.spectrumTexture);
        sosShader.SetInt(Shader.PropertyToID("_NumOfSines"), numOfSines);
        sosShader.SetFloat(Shader.PropertyToID("_Time"), Time.fixedTime);
    }

    void SetMaterialVariables(){
        Vector3 lightDir = mainLight.forward.normalized;
        lightDir.y = -lightDir.y;
        material.SetVector("_SunDirection", lightDir);
    }
}
