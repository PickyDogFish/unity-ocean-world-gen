
using UnityEngine;

public class TerrainGen : MonoBehaviour
{
    private static Vector2 tileAmount = Vector2.one;
    private ComputeShader noiseCS;
    private float width = 1000;
    private float length = 1000;
    private float height = 600;

    private int heightmapResolution = 513; //apparently has to be one more
    private int detailResolution = 1024;
    private int detailResolutionPerPatch = 8;
    private int controlTextureResolution = 512;
    private int baseTextureResolution = 1024;

    private string path = string.Empty;

    void Awake()
    {
        noiseCS = Resources.Load<ComputeShader>("NoiseGenerator");
    }
    void Start()
    {
        CreateTerrainTile(Vector2Int.zero);
    }

    private void CreateTerrainTile(Vector2Int tileCoords)
    {



        TerrainData terrainData = new TerrainData();

        terrainData.size = new Vector3(width / 16f,
                                        height,
                                        length / 16f);

        terrainData.baseMapResolution = baseTextureResolution;
        terrainData.heightmapResolution = heightmapResolution;
        Texture2D noise = GaussianNoise.GenerateTex(512);
        terrainData.SetHeights(0, 0, NoiseGen.GetNoiseArray(noiseCS, heightmapResolution, 1));

        terrainData.alphamapResolution = controlTextureResolution;
        terrainData.SetDetailResolution(detailResolution, detailResolutionPerPatch);

        terrainData.name = name;
        GameObject terrain = (GameObject)Terrain.CreateTerrainGameObject(terrainData);

        terrain.name = name;
        terrain.transform.parent = gameObject.transform;
        terrain.transform.position = new Vector3(tileCoords.x * width, 0, tileCoords.y * length);
    }



}