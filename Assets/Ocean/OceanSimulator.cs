using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;

public class OceanSimulator : MonoBehaviour
{

    [SerializeField] private RenderTexture initialSpectrum;
    [SerializeField] private RenderTexture timeSpectrum;
    [SerializeField] private OceanParameters oceanParameters;
    [SerializeField] private ComputeShader spectrumShader;

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


        //time dependent spectrum
        timeSpectrum = CreateRenderTexture(oceanParameters.size, 1);

    }

    // Update is called once per frame
    void Update()
    {
        spectrumShader.SetFloat(Shader.PropertyToID("_FrameTime"), Time.time);
        spectrumShader.SetTexture(2, Shader.PropertyToID("_InitialSpectrum"), initialSpectrum);
        spectrumShader.SetTexture(2, Shader.PropertyToID("_TimeSpectrum"), timeSpectrum);
        spectrumShader.Dispatch(2, oceanParameters.size / workGroupsX, oceanParameters.size / workGroupsY, 1);
    }

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
