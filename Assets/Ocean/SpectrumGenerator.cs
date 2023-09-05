using UnityEngine;
using System;

namespace OceanSimulation
{
    public class SpectrumGenerator : MonoBehaviour
    {
        System.Random rand = new System.Random();
        [SerializeField] private OceanParameters oceanParameters;
        public Texture2D spectrumTexture;

        public Vector4[,] spectrum;

        // Start is called before the first frame update
        void Start()
        {
            CalculateInitialSpectrum();

            /* spectrumTexture = CreateTexture(oceanParameters.resolution);
            for (int i = 0; i < oceanParameters.resolution; i++)
            {
                for (int j = 0; j < oceanParameters.resolution; j++)
                {

                    //spectrumTexture.SetPixel(i, j, spectrum[i,j]);
                    Vector2 timeSpectrum = TimeSpectrum(1.23f, i, j);
                    spectrumTexture.SetPixel(i, j, new Color(timeSpectrum.x, timeSpectrum.y, 0, 0));
                }
            }
            spectrumTexture.Apply(); */
        }

        void Update()
        {
            spectrumTexture = CreateTexture(oceanParameters.size);
            for (int i = 0; i < oceanParameters.size; i++)
            {
                for (int j = 0; j < oceanParameters.size; j++)
                {

                    //spectrumTexture.SetPixel(i, j, spectrum[i,j]);
                    Vector2 timeSpectrum = TimeSpectrum(Time.deltaTime, i, j);
                    spectrumTexture.SetPixel(i, j, new Color(timeSpectrum.x, timeSpectrum.y, 0, 0));
                }
            }
            spectrumTexture.Apply();
        }


        void CalculateInitialSpectrum()
        {
            spectrum = new Vector4[oceanParameters.size, oceanParameters.size];
            for (int i = 0; i < oceanParameters.size; i++)
            {
                for (int j = 0; j < oceanParameters.size; j++)
                {
                    Vector2 h = RandomTimesSpectrum(i, j);
                    Vector2 minush = RandomTimesSpectrum(oceanParameters.size - i, oceanParameters.size - j);
                    spectrum[i, j] = new Vector4(h.x, h.y, minush.x, -minush.y);
                }
            }
        }

        Vector2 RandomTimesSpectrum(int n_prime, int m_prime)
        {
            float spectrumVal = CalcPM(n_prime, m_prime);
            return new Vector2(spectrumVal * GaussianRandom(), spectrumVal * GaussianRandom());
        }

        //Box muller method
        float GaussianRandom()
        {
            Vector2 res = Vector2.zero;
            double u1 = 1.0 - rand.NextDouble();
            double u2 = 1.0 - rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            return (float)randStdNormal;
        }

        /// <summary>
        /// Gets the Pierson-Moskowitz spectrum value for grid position n,m.
        /// </summary>
        float CalcPM(int n_prime, int m_prime)
        {
            Vector2 k = new Vector2(Mathf.PI * (2 * n_prime - oceanParameters.size) / oceanParameters.lengthScale, Mathf.PI * (2 * m_prime - oceanParameters.size) / oceanParameters.lengthScale);
            float kLength = k.magnitude * 250;
            if (kLength < 0.000001f) return 0.0f;
            float omega = Frequency(kLength, oceanParameters.depth);
            float peakOmega = PiersonMoskowitzPeakOmega(oceanParameters.windSpeed.magnitude);
            float k_dot_w = Vector2.Dot(k.normalized, oceanParameters.windSpeed.normalized);
            float k_dot_w2 = k_dot_w * k_dot_w * k_dot_w * k_dot_w * k_dot_w * k_dot_w;
            return PiersonMoskowitz(omega, peakOmega, k_dot_w2);
        }

        //g * k - deep water dispersion
        //sigmaOverRho * k^3 - surface tension

        //https://uwaterloo.ca/applied-mathematics/current-undergraduates/continuum-and-fluid-mechanics-students/amath-463-students/surface-gravity-waves
        float Frequency(float k, float depth)
        {
            float dispersion = OceanParameters.GRAVITY * k;
            float surfaceTension = OceanParameters.sigmaOverRho * k * k * k;
            float result = Mathf.Sqrt((dispersion + surfaceTension) * (float)Math.Tanh(Mathf.Min(k * depth, 10)));
            return result;
        }
        float PiersonMoskowitzPeakOmega(float u)
        {
            float nu = 0.13f;
            return 2 * Mathf.PI * nu * OceanParameters.GRAVITY / u;
        }


        float PiersonMoskowitz(float omega, float peakOmega, float angle)
        {

            float oneOverOmega = 1 / omega;
            float peakOmegaOverOmega = peakOmega / omega;

            return oceanParameters.waveAmplitude * 100000 * 8.1e-3f * OceanParameters.GRAVITY * OceanParameters.GRAVITY * oneOverOmega * oneOverOmega * oneOverOmega * oneOverOmega * oneOverOmega
                * Mathf.Exp(-1.25f * peakOmegaOverOmega * peakOmegaOverOmega * peakOmegaOverOmega * peakOmegaOverOmega) * angle;
        }
        Texture2D CreateTexture(int dim)
        {
            return new Texture2D(dim, dim, TextureFormat.RGBAFloat, false);
        }

        Vector2 TimeSpectrum(float t, int n_prime, int m_prime)
        {
            Vector2 k = new Vector2(Mathf.PI * (2 * n_prime - oceanParameters.size) / oceanParameters.lengthScale, Mathf.PI * (2 * m_prime - oceanParameters.size) / oceanParameters.lengthScale);

            //dispersion
            float omegat = Dispersion(n_prime, m_prime) * t;
            float cosinus = Mathf.Cos(omegat);
            float sinus = Mathf.Sin(omegat);

            Vector2 hke = ComplexMultiply(spectrum[n_prime, m_prime].x, spectrum[n_prime, m_prime].y, cosinus, sinus);
            Vector2 hminuske = ComplexMultiply(spectrum[n_prime, m_prime].z, spectrum[n_prime, m_prime].w, cosinus, -sinus);

            return hke + hminuske;
        }

        float Dispersion(int n_prime, int m_prime)
        {
            float w_0 = 2.0f * Mathf.PI / 200.0f;
            float kx = Mathf.PI * (2 * n_prime - oceanParameters.size) / oceanParameters.lengthScale;
            float kz = Mathf.PI * (2 * m_prime - oceanParameters.size) / oceanParameters.lengthScale;
            return Mathf.Floor(Mathf.Sqrt(OceanParameters.GRAVITY * Mathf.Sqrt(kx * kx + kz * kz)) / w_0) * w_0;
        }

        Vector2 ComplexMultiply(float a0, float b0, float a1, float b1)
        {
            return new Vector2(a0 * a1 - b0 * b1, a0 * b1 + b0 * a1);
        }
    }


}