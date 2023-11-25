
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using UnityEditor;
using UnityEngine;
using UnityEngine.TerrainUtils;

public class TerrainGen : MonoBehaviour
{
    private ComputeShader noiseCS;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private int tileRange = 1;

    [Header("Chunk settings")]
    [SerializeField] private float widthScale = 512;
    [SerializeField] private float lengthScale = 512;
    [SerializeField] private float heightScale = 256;
    private Vector3 size {get {return new Vector3(widthScale, heightScale, lengthScale);}}

    [SerializeField] private float noiseScale = 1;


    [Header("Texture settings")]
    [SerializeField] TerrainLayer rockLayer;
    [SerializeField] TerrainLayer grassLayer;
    [SerializeField] TerrainLayer sandLayer;

    private int heightmapResolution = 513; //apparently has to be one more
    private int detailResolution = 1024;
    private int detailResolutionPerPatch = 8;
    private int alphamapResolution = 512;
    private int baseTextureResolution = 1024;



    private Dictionary<Vector2Int, Terrain> tileDictionary = new Dictionary<Vector2Int, Terrain>();

    void Awake()
    {
        noiseCS = Resources.Load<ComputeShader>("NoiseGenerator");
    }
    void Start()
    {
        for (int x = -tileRange; x <= tileRange; x++)
        {
            for (int y = -tileRange; y <= tileRange; y++)
            {
                AddTerrain(new Vector2Int(x,y));
            }
        }
    }

    public void AddTerrain(Vector2Int terrainCoords){
        Terrain terrain = CreateTerrainTile(terrainCoords);
        tileDictionary.Add(terrainCoords, terrain);
        Vector2Int neighbour = terrainCoords + new Vector2Int(-1,0);
        if (tileDictionary.ContainsKey(neighbour)){
            Terrain leftTile = tileDictionary[neighbour];
            terrain.SetNeighbors(leftTile, terrain.topNeighbor, terrain.rightNeighbor, terrain.bottomNeighbor);
            leftTile.SetNeighbors(leftTile.leftNeighbor, leftTile.topNeighbor, terrain, leftTile.bottomNeighbor);
        }
        neighbour = terrainCoords + new Vector2Int(0,1);
        if (tileDictionary.ContainsKey(neighbour)){
            Terrain topTile = tileDictionary[neighbour];
            terrain.SetNeighbors(terrain.leftNeighbor, topTile, terrain.rightNeighbor, terrain.bottomNeighbor);
            topTile.SetNeighbors(topTile.leftNeighbor, topTile.topNeighbor, topTile.rightNeighbor, terrain);
        }
        neighbour = terrainCoords + new Vector2Int(1,0);
        if (tileDictionary.ContainsKey(neighbour)){
            Terrain rightTile = tileDictionary[neighbour];
            terrain.SetNeighbors(terrain.leftNeighbor, terrain.topNeighbor, rightTile, terrain.bottomNeighbor);
            rightTile.SetNeighbors(terrain, rightTile.topNeighbor, rightTile.rightNeighbor, rightTile.bottomNeighbor);
        }
        neighbour = terrainCoords + new Vector2Int(0,-1);
        if (tileDictionary.ContainsKey(neighbour)){
            Terrain bottomTile = tileDictionary[neighbour];
            terrain.SetNeighbors(terrain.leftNeighbor, terrain.topNeighbor, terrain.rightNeighbor, bottomTile);
            bottomTile.SetNeighbors(bottomTile.leftNeighbor, terrain, bottomTile.rightNeighbor, bottomTile.bottomNeighbor);
        }
    }

    private void PrintNeighbors(Vector2Int terrainCoords){
        Debug.Log(terrainCoords + "    left: " + tileDictionary[terrainCoords].leftNeighbor + "    top: " + tileDictionary[terrainCoords].topNeighbor + "    right: " + tileDictionary[terrainCoords].rightNeighbor + "    bottom: " + tileDictionary[terrainCoords].bottomNeighbor);
    }


    public Terrain CreateTerrainTile(Vector2Int terrainCoords){
        GameObject newTerrainGO = Terrain.CreateTerrainGameObject(CreateTerrainData(terrainCoords));
        newTerrainGO.name = "Terrain" + terrainCoords.ToString();
        newTerrainGO.transform.parent = gameObject.transform;

        Vector3 pos = new Vector3(terrainCoords.x, 0, terrainCoords.y);
        pos.Scale(size);
        newTerrainGO.transform.localPosition = pos;

        Terrain terrain = newTerrainGO.GetComponent<Terrain>();
        terrain.groupingID = 1;
        terrain.allowAutoConnect = false;
        return terrain;
    }

    private TerrainData CreateTerrainData(Vector2Int tileCoords)
    {
        TerrainData terrainData = new TerrainData();

        terrainData.size = new Vector3(widthScale / 16f,
                                        heightScale,
                                        lengthScale / 16f);

        terrainData.baseMapResolution = baseTextureResolution;
        terrainData.heightmapResolution = heightmapResolution;
        float[,] heights = NoiseGen.GetNoiseArray(tileCoords, noiseCS, heightmapResolution, noiseScale);
        terrainData.SetHeights(0, 0, heights);

        terrainData.alphamapResolution = alphamapResolution;
        terrainData.SetDetailResolution(detailResolution, detailResolutionPerPatch);

        terrainData.name = name + tileCoords.ToString();
        terrainData.terrainLayers = new TerrainLayer[3]{rockLayer, grassLayer, sandLayer};
        terrainData.SetAlphamaps(0,0, CreateAlphaMap(terrainData, heights));

        return terrainData;
    }

    private float[,,] CreateAlphaMap(TerrainData terrainData, float[,] heights){

        float[,,] map = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, 3];

        // For each point on the alphamap...
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                // Get the normalized terrain coordinate that
                // corresponds to the point.
                float normX = x * 1.0f / (terrainData.alphamapWidth - 1);
                float normY = y * 1.0f / (terrainData.alphamapHeight - 1);

                // Get the steepness value at the normalized coordinate.
                float angle = terrainData.GetSteepness(normX, normY);
                float height = WorldSpaceHeight(heights[x,y]);

                // Steepness is given as an angle, 0..90 degrees. Divide
                // by 90 to get an alpha blending value in the range 0..1.
                if (height < 0){
                    map[x, y, 0] = 0;
                    map[x, y, 1] = 0;
                    map[x,y,2] = 1;
                } else {
                    float frac = angle / 90.0f;
                    map[x, y, 0] = frac;
                    map[x, y, 1] = 1 - frac;
                }
            }
        }
        return map;
    }

    private float WorldSpaceHeight(float height){
        return height * heightScale + transform.position.y;
    }
}