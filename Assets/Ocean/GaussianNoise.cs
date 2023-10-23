using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class GaussianNoise
{
    /// <summary>
    /// Calculates gaussian noise and write it to a Texture2D. Uses the Box-Mueller method
    /// </summary>
    /// <returns>
    /// Texture2D of format RGFloat
    /// </returns>
    public static Texture2D GenerateTex(int dim, float mean = 0, float stdDev = 1){
        Texture2D res = new Texture2D(dim, dim, TextureFormat.RGFloat, false);
        System.Random rand = new System.Random(); //reuse this if you are generating many
        for (int i = 0; i < dim; i++)
        {
            for (int j = 0; j < dim; j++)
            {
                float u1 = (float) (1.0- rand.NextDouble()); //uniform(0,1] random doubles
                float u2 = (float) (1.0- rand.NextDouble());
                float part1 = Mathf.Sqrt(-2.0f * Mathf.Log(u1));
                float part2 = 2.0f * Mathf.PI * u2;
                float randStdNormal1 = part1 * Mathf.Sin(part2); //random normal(0,1)
                float randNormal1 = mean + stdDev * randStdNormal1; //random normal(mean,stdDev^2)
                float randStdNormal2 = part1 * Mathf.Cos(part2); //random normal(0,1)
                float randNormal2 = mean + stdDev * randStdNormal2; //random normal(mean,stdDev^2)
                Color c = new Color(randNormal1,randNormal2,0,0);
                res.SetPixel(i, j, c);
            }
        }
        res.Apply();
        return res;
    }

}
