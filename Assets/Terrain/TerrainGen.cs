
using System.ComponentModel;
using System.Drawing;
using UnityEngine;
using UnityEngine.TerrainUtils;

public class TerrainGen : MonoBehaviour
{
    private ComputeShader noiseCS;
    [SerializeField] private float width = 512;
    [SerializeField] private float length = 512;
    [SerializeField] private float height = 256;
    private Vector3 size {get {return new Vector3(width, height, length);}}

    private int heightmapResolution = 513; //apparently has to be one more
    private int detailResolution = 1024;
    private int detailResolutionPerPatch = 8;
    private int controlTextureResolution = 512;
    private int baseTextureResolution = 1024;

    void Awake()
    {
        noiseCS = Resources.Load<ComputeShader>("NoiseGenerator");
    }
    void Start()
    {
        Terrain startingTerrain = CreateTerrain(Vector2Int.zero);
        startingTerrain.SetNeighbors(CreateTerrain(Vector2Int.left), CreateTerrain(Vector2Int.up), CreateTerrain(Vector2Int.right), CreateTerrain(Vector2Int.down));
    }

    

    public Terrain CreateTerrain(Vector2Int terrainCoords){
        GameObject newTerrainGO = Terrain.CreateTerrainGameObject(CreateTerrainData(terrainCoords));
        newTerrainGO.name = "Terrain" + terrainCoords.ToString();
        newTerrainGO.transform.parent = gameObject.transform;
        Vector3 pos = new Vector3(terrainCoords.x, 0, terrainCoords.y);
        pos.Scale(size);
        newTerrainGO.transform.position = pos;
        return newTerrainGO.GetComponent<Terrain>();
    }

    private TerrainData CreateTerrainData(Vector2Int tileCoords)
    {
        TerrainData terrainData = new TerrainData();

        terrainData.size = new Vector3(width / 16f,
                                        height,
                                        length / 16f);

        terrainData.baseMapResolution = baseTextureResolution;
        terrainData.heightmapResolution = heightmapResolution;
        terrainData.SetHeights(0, 0, NoiseGen.GetNoiseArray(tileCoords, noiseCS, heightmapResolution, 1));

        terrainData.alphamapResolution = controlTextureResolution;
        terrainData.SetDetailResolution(detailResolution, detailResolutionPerPatch);

        terrainData.name = name + tileCoords.ToString();
        
        return terrainData;
    }



}