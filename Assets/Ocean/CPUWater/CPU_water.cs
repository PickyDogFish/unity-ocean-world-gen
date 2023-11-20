using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using OceanSimulation;
using Unity.Mathematics;
using UnityEngine;

public class CPU_water : MonoBehaviour
{
    [SerializeField] SpectrumGenerator spectrumGen;
    [SerializeField] private int oceanSize = 32;
    [SerializeField] private float oceanScale = 1;
    [SerializeField] private float waveAmplitude = 1;
    private float[,] heightMap;
    [SerializeField] Texture2D heightTex;
    [SerializeField] Texture2D testTex;
    [SerializeField] RenderTexture heightRT;

    [SerializeField] Material oceanMaterial;

    [SerializeField] bool runOnGPU = false;
    [SerializeField] private ComputeShader heightShader;

    void Awake()
    {
        spectrumGen = GetComponent<SpectrumGenerator>();
        heightMap = new float[oceanSize, oceanSize];
        heightTex = new Texture2D(oceanSize, oceanSize, TextureFormat.RFloat, false);
        heightRT = new RenderTexture(oceanSize, oceanSize, 0, RenderTextureFormat.RFloat);
        heightRT.enableRandomWrite = true;
    }
    // Start is called before the first frame update
    void Start()
    {
        if (testTex != null){
            oceanMaterial.SetTexture(Shader.PropertyToID("_HeightMap"), testTex);
        }
        else if (!runOnGPU){
            oceanMaterial.SetTexture(Shader.PropertyToID("_HeightMap"), heightTex);
            CalculateHeight();
            WriteToTex();
        } else {
            oceanMaterial.SetTexture(Shader.PropertyToID("_HeightMap"), heightRT);
            CalculateHeightGPU();
        }
    }

    void CalculateHeight()
    {
        int n = oceanSize;
        for (int hx = 0; hx < n; hx++)
        {
            for (int hy = 0; hy < n; hy++)
            {
                float height = 0;
                for (int x = 0; x < n; x++)
                {
                    for (int y = 0; y < n; y++)
                    {
                        //k represents the frequency
                        Vector2 k = new Vector2(x - n / 2, y - n / 2) * Mathf.PI / oceanScale;
                        Vector4 spectrum = spectrumGen.timeSpectrum[y,x];
                        float a = spectrum[0];
                        float b = spectrum[1];
                        //float A = Mathf.Sqrt(a * a + b * b);
                        //float phi = Mathf.Atan2(b, a);

                        float kdotx = Vector2.Dot(k, new Vector2(hx, hy)) + 0.1f;
                        Vector2 c = Euler(kdotx);

                        Vector2 htilde = ComplexMult(new Vector2(a, b), c);

                        //float value = A * Mathf.Sin(2 * Mathf.PI * k.magnitude + phi);
                        height += htilde.x;
                    }
                }
                Debug.Log(height);
                heightMap[hx, hy] = height * waveAmplitude;
            }
        }
    }

    void WriteToTex()
    {
        int n = oceanSize;
        for (int hx = 0; hx < n; hx++)
        {
            for (int hy = 0; hy < n; hy++)
            {
                heightTex.SetPixel(hx,hy, new Color(heightMap[hx,hy], 0, 0));
            }
        }
        heightTex.Apply();
    }


    Vector2 Euler(float a)
    {
        return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    Vector2 ComplexMult(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
    }

    float updateCycle = 0.1f;
    float curTime = 0;

    // Update is called once per frame
    void Update()
    {
        curTime += Time.deltaTime;
        if (!runOnGPU && curTime > updateCycle){
            Debug.Log("Updating");
            curTime = 0;
            CalculateHeight();
            WriteToTex();
        } else if (runOnGPU){
            CalculateHeightGPU();
        }
    }

    void CalculateHeightGPU(){
        SetupShader();
        heightShader.Dispatch(0, oceanSize/8, oceanSize/8, 1);
    }

    void SetupShader(){
        heightShader.SetTexture(0, "_TimeSpectrum", spectrumGen.spectrumTexture);
        heightShader.SetTexture(0, "_HeightMap", heightRT);
        heightShader.SetInt("_Size", oceanSize);
        heightShader.SetFloat("_LengthScale", oceanScale);
        heightShader.SetFloat("_Amplitude", waveAmplitude);
    }
}