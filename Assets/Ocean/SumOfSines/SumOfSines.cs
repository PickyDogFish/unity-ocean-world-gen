using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

public class SumOfSines : MonoBehaviour
{
    [SerializeField] private int texSize = 256, numOfSines = 4;
    [SerializeField] private float heightMult = 1;
    [SerializeField] private RenderTexture heightTex, normalTex;

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
