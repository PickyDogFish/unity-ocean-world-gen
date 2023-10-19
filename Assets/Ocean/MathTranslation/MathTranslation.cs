using OceanSimulation;
using UnityEngine;

public class MathTranslation : MonoBehaviour
{
    [SerializeField] private Transform mainLight;
    [SerializeField] private int texSize = 128;
    [SerializeField] private RenderTexture heightTex;
    [SerializeField] private RenderTexture normalTex;
    [SerializeField] public Material material;
    [SerializeField] private ComputeShader mathShader;
    [SerializeField] private float len = 128;
    [SerializeField] private Vector2 wind = new Vector2(5,2);
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
        //mathShader.SetTexture(0, Shader.PropertyToID("_NormalTex"), normalTex);
        //mathShader.SetInt(Shader.PropertyToID("_TexSize"), texSize);
        //mathShader.SetTexture(0, Shader.PropertyToID("_SpectrumTex"), spectrumGen.spectrumTexture);
        //mathShader.SetInt(Shader.PropertyToID("_NumOfSines"), numOfSines);
        mathShader.SetInt(Shader.PropertyToID("N"), texSize);
        mathShader.SetFloat(Shader.PropertyToID("len"), len);
        mathShader.SetVector(Shader.PropertyToID("wind"), wind);
        mathShader.SetFloat(Shader.PropertyToID("_Time"), Time.fixedTime);
    }

    void SetMaterialVariables(){
        material.SetTexture("_HeightMap", heightTex);
        //material.SetTexture("_NormalMap", normalTex);
        Vector3 lightDir = mainLight.forward.normalized;
        lightDir.y = -lightDir.y;
        material.SetVector("_SunDirection", lightDir);
    }
}
