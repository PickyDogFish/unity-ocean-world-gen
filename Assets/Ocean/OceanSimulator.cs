using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;

public class OceanSimulator : MonoBehaviour
{

    [SerializeField] private RenderTexture initialSpectrum;
    [SerializeField] private RenderTexture timeSpectrum;
    [SerializeField] private RenderTexture heightTex;
    [SerializeField] private OceanParameters oceanParameters;
    [SerializeField] private ComputeShader spectrumShader;
    [SerializeField] private ComputeShader fftShader;

    private int workGroupsX = 8, workGroupsY = 8;

    // Start is called before the first frame update
    void Start()
    {
        initialSpectrum = CreateRenderTexture(oceanParameters.size, 1);

        spectrumShader.SetInt(Shader.PropertyToID("_Size"), oceanParameters.size);
        spectrumShader.SetFloat(Shader.PropertyToID("_WindSpeed"), oceanParameters.windSpeed.magnitude);
        spectrumShader.SetFloat(Shader.PropertyToID("_Scale"), oceanParameters.lengthScale);
        spectrumShader.SetFloat(Shader.PropertyToID("_Depth"), oceanParameters.depth);

        spectrumShader.SetTexture(0, Shader.PropertyToID("_InitialSpectrum"), initialSpectrum);
        spectrumShader.Dispatch(0, oceanParameters.size / workGroupsX, oceanParameters.size / workGroupsY, 1);
        spectrumShader.SetTexture(1, Shader.PropertyToID("_InitialSpectrum"), initialSpectrum);
        spectrumShader.Dispatch(1, oceanParameters.size / workGroupsX, oceanParameters.size / workGroupsY, 1);


        timeSpectrum = CreateRenderTexture(oceanParameters.size, 1);

        heightTex = CreateRenderTexture(oceanParameters.size, 1);


    }

    // Update is called once per frame
    bool ran = false;
    void Update()
    {
        if (!ran)
        {
            //ran = true;
            //time dependent spectrum
            spectrumShader.SetFloat(Shader.PropertyToID("_FrameTime"), Time.time);
            spectrumShader.SetTexture(2, Shader.PropertyToID("_InitialSpectrum"), initialSpectrum);
            spectrumShader.SetTexture(2, Shader.PropertyToID("_TimeSpectrum"), timeSpectrum);
            spectrumShader.Dispatch(2, oceanParameters.size / workGroupsX, oceanParameters.size / workGroupsY, 1);

            //IDFT
            fftShader.SetTexture(0, Shader.PropertyToID("_TimeSpectrum"), timeSpectrum);
            fftShader.SetTexture(0, Shader.PropertyToID("_HeightTex"), heightTex);
            fftShader.SetInt(Shader.PropertyToID("_Size"), oceanParameters.size);
            fftShader.SetFloat(Shader.PropertyToID("_Scale"), oceanParameters.lengthScale);
            fftShader.Dispatch(0, oceanParameters.size / workGroupsX, oceanParameters.size / workGroupsY, 1);
        }

    }

    

    /*     void DFT()
        {

            float halfN = oceanParameters.size / 2.0f;

            Vector2 x = id.xy - halfN;

            float2 h = 0.0f;
            for (uint m = 0; m < _Size; ++m)
            {
                float kz = 2.0f * OCEANOGRAPHY_PI * (m - halfN) / _Scale;
                for (uint n = 0; n < _Size; ++n)
                {
                    float kx = 2.0f * OCEANOGRAPHY_PI * (n - halfN) / _Scale;
                    float2 K = float2(kx, kz);
                    float kMag = length(K);
                    float kdotx = dot(K, x);

                    float2 c = EulerFormula(kdotx);
                    float2 htilde = ComplexMult(_TimeSpectrum[uint3(n, m, 0)], c);
                    if (kMag < 0.001f) htilde = 0.0f;
                    h = h + htilde;
                }
            }
        } */


    RenderTexture CreateRenderTexture(int size, int cascadeNumber)
    {
        RenderTextureDescriptor initialsDescriptor = new RenderTextureDescriptor()
        {
            height = size,
            width = size,
            volumeDepth = cascadeNumber,
            enableRandomWrite = true,
            colorFormat = RenderTextureFormat.ARGBHalf,
            sRGB = false,
            msaaSamples = 1,
            depthBufferBits = 0,
            useMipMap = false,
            dimension = TextureDimension.Tex2DArray
        };
        RenderTexture rt = new RenderTexture(initialsDescriptor);
        rt.Create();
        return rt;
    }
}
