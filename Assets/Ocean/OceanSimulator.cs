using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class OceanSimulator : MonoBehaviour
{

    [SerializeField] private RenderTexture initialSpectrum;
    [SerializeField] private OceanParameters oceanParameters;
    // Start is called before the first frame update
    void Start()
    {
        initialSpectrum = CreateRenderTexture(oceanParameters.size, 1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    RenderTexture CreateRenderTexture(int size, int cascadeNumber){
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
