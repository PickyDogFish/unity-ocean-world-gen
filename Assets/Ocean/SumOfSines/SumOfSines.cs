using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

public class SumOfSines : MonoBehaviour
{
    [SerializeField] private int texSize = 256, numOfSines = 4;
    [SerializeField] private RenderTexture heightTex, normalTex;

    [SerializeField] public Material heightMaterial;

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
        heightMaterial.SetTexture("_HeightMap", heightTex);
    }

    // Update is called once per frame
    void Update()
    {
        SetVariables();
        sosShader.Dispatch(0, texSize/8, texSize/8, 1);
    }

    void SetVariables(){
        sosShader.SetTexture(0, Shader.PropertyToID("_HeightTex"), heightTex);
        sosShader.SetTexture(0, Shader.PropertyToID("_NormalTex"), normalTex);
        sosShader.SetInt(Shader.PropertyToID("_TexSize"), texSize);
        sosShader.SetInt(Shader.PropertyToID("_NumOfSines"), numOfSines);
        sosShader.SetFloat(Shader.PropertyToID("_Time"), Time.fixedTime);
    }
}
