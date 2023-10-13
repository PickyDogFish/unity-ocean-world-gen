using UnityEngine;

public class SumOfSines : MonoBehaviour
{
    [SerializeField] private Transform mainLight;
    [SerializeField] private int texSize = 256, numOfSines = 4;
    [SerializeField] private float heightMult = 1;
    private RenderTexture heightTex;
    [SerializeField] private RenderTexture normalTex;

    [SerializeField] public Material material;

    [SerializeField] private ComputeShader sosShader;
    void Start()
    {
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
        Vector3 lightDir = mainLight.forward.normalized;
        lightDir.y = -lightDir.y;
        material.SetVector("_SunDirection", lightDir);
        Debug.Log(mainLight.forward.normalized);

        sosShader.SetTexture(0, Shader.PropertyToID("_HeightTex"), heightTex);
        sosShader.SetTexture(0, Shader.PropertyToID("_NormalTex"), normalTex);
        sosShader.SetInt(Shader.PropertyToID("_TexSize"), texSize);
    }

    // Update is called once per frame
    void Update()
    {
        SetVariables();
        material.SetFloat("_HeightMult", heightMult);
        sosShader.Dispatch(0, texSize/8, texSize/8, 1);
    }

    void SetVariables(){
        sosShader.SetInt(Shader.PropertyToID("_NumOfSines"), numOfSines);
        sosShader.SetFloat(Shader.PropertyToID("_Time"), Time.fixedTime);
    }
}
