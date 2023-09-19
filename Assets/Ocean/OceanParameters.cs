using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OceanParameters", menuName = "Ocean/Parameters")]
public class OceanParameters : ScriptableObject
{
    public const float GRAVITY = 9.81f;
    public const float sigmaOverRho = 0.074e-3f;

    /// <summary>
    /// The fourier size. Must be a pow2 number.
    /// </summary>
    public int size = 256;

    /// <summary>
    /// The wind speed and direction.
    /// </summary>
    [SerializeField]
    public Vector2 windSpeed = new Vector2(32.0f, 32.0f);
    

    /// <summary>
    /// The depth of sea for the pierson-moskowitz spectrum.
    /// </summary>
    [SerializeField]
    [Tooltip("The ocean depth for Pierson-Moskowitz")]
    public float depth = 1000;

    [SerializeField]
    public float waveAmplitude = 1;

    public float lengthScale = 512;
}
