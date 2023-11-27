
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TerrainUtils;

public class TerrainGen : MonoBehaviour
{
    private ComputeShader noiseCS;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private int tileRange = 1;

    [Header("Overall settings")]
    [SerializeField] private float noiseScale = 1;
    [Range(0,1)][SerializeField] private float percentUnderwater = 0.6f;

    [Header("Chunk settings")]
    [SerializeField] private int widthScale = 64;
    [SerializeField] private int heightScale = 64;
    private Vector3 size { get { return new Vector3(widthScale, heightScale, widthScale); } }



    [Header("Texture settings")]
    [SerializeField] TerrainLayer rockLayer;
    [SerializeField] TerrainLayer grassLayer;
    [SerializeField] TerrainLayer sandLayer;

    private int heightmapResolution { get { return widthScale + 1; } } //apparently has to be one more
    private int detailResolution { get { return widthScale; } }
    private int detailResolutionPerPatch = 8;
    private int alphamapResolution { get { return widthScale + 1; } }
    private int baseTextureResolution { get { return widthScale / 2; } }



    private Dictionary<Vector2Int, Terrain> generatedTileDictionary = new Dictionary<Vector2Int, Terrain>();
    private Dictionary<Vector2Int, Terrain> shownTileDictionary = new Dictionary<Vector2Int, Terrain>();

    void Awake()
    {
        noiseCS = Resources.Load<ComputeShader>("NoiseGenerator");
    }
    void Start()
    {
        transform.position = new Vector3(0, -heightScale * percentUnderwater, 0);

        foreach (Vector2Int chunkCoord in ChunkCoordsInRange(cameraTransform.position, tileRange))
        {
            AddTerrain(chunkCoord);
        }
    }

    private void PrintArray(Array array)
    {
        Debug.Log(array);
        foreach (var item in array)
        {
            Debug.Log(item);
        }
    }


    void Update()
    {
        List<Vector2Int> tilesInRange = ChunkCoordsInRange(cameraTransform.position, tileRange);
        foreach (Vector2Int newChunk in tilesInRange)
        {
            ShowTerrain(newChunk);
        }
        List<Vector2Int> chunksToHide = shownTileDictionary.Keys.Except(tilesInRange).ToList();
        foreach (Vector2Int outOfRangeTile in chunksToHide){
            HideTerrain(outOfRangeTile);
        }
    }
    private void PrintNeighbors(Vector2Int terrainCoords)
    {
        Debug.Log(terrainCoords + "    left: " + generatedTileDictionary[terrainCoords].leftNeighbor + "    top: " + generatedTileDictionary[terrainCoords].topNeighbor + "    right: " + generatedTileDictionary[terrainCoords].rightNeighbor + "    bottom: " + generatedTileDictionary[terrainCoords].bottomNeighbor);
    }

    public void ShowTerrain(Vector2Int terrainCoords) {
        if (generatedTileDictionary.ContainsKey(terrainCoords)){
            Terrain tile = generatedTileDictionary[terrainCoords];
            tile.gameObject.SetActive(true);
            shownTileDictionary.TryAdd(terrainCoords, tile);
        } else {
            AddTerrain(terrainCoords);
        }
    }

    public void HideTerrain(Vector2Int terrainCoords)
    {
        if (shownTileDictionary.ContainsKey(terrainCoords))
        {
            Terrain tile = shownTileDictionary[terrainCoords];
            tile.gameObject.SetActive(false);
            shownTileDictionary.Remove(terrainCoords);
        }
    }

    public void AddTerrain(Vector2Int terrainCoords)
    {
        Terrain terrain = CreateTerrainTile(terrainCoords);
        generatedTileDictionary.Add(terrainCoords, terrain);
        shownTileDictionary.Add(terrainCoords, terrain);
        Vector2Int neighbour = terrainCoords + new Vector2Int(-1, 0);
        if (generatedTileDictionary.ContainsKey(neighbour))
        {
            Terrain leftTile = generatedTileDictionary[neighbour];
            terrain.SetNeighbors(leftTile, terrain.topNeighbor, terrain.rightNeighbor, terrain.bottomNeighbor);
            leftTile.SetNeighbors(leftTile.leftNeighbor, leftTile.topNeighbor, terrain, leftTile.bottomNeighbor);
        }
        neighbour = terrainCoords + new Vector2Int(0, 1);
        if (generatedTileDictionary.ContainsKey(neighbour))
        {
            Terrain topTile = generatedTileDictionary[neighbour];
            terrain.SetNeighbors(terrain.leftNeighbor, topTile, terrain.rightNeighbor, terrain.bottomNeighbor);
            topTile.SetNeighbors(topTile.leftNeighbor, topTile.topNeighbor, topTile.rightNeighbor, terrain);
        }
        neighbour = terrainCoords + new Vector2Int(1, 0);
        if (generatedTileDictionary.ContainsKey(neighbour))
        {
            Terrain rightTile = generatedTileDictionary[neighbour];
            terrain.SetNeighbors(terrain.leftNeighbor, terrain.topNeighbor, rightTile, terrain.bottomNeighbor);
            rightTile.SetNeighbors(terrain, rightTile.topNeighbor, rightTile.rightNeighbor, rightTile.bottomNeighbor);
        }
        neighbour = terrainCoords + new Vector2Int(0, -1);
        if (generatedTileDictionary.ContainsKey(neighbour))
        {
            Terrain bottomTile = generatedTileDictionary[neighbour];
            terrain.SetNeighbors(terrain.leftNeighbor, terrain.topNeighbor, terrain.rightNeighbor, bottomTile);
            bottomTile.SetNeighbors(bottomTile.leftNeighbor, terrain, bottomTile.rightNeighbor, bottomTile.bottomNeighbor);
        }
    }

    public Terrain CreateTerrainTile(Vector2Int terrainCoords)
    {
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

        terrainData.size = new Vector3(widthScale / 2f,
                                        heightScale,
                                        widthScale / 2f);

        terrainData.baseMapResolution = baseTextureResolution;
        terrainData.heightmapResolution = heightmapResolution;
        float[,] heights = NoiseGen.GetNoiseArray(tileCoords, noiseCS, heightmapResolution, noiseScale);
        terrainData.SetHeights(0, 0, heights);

        terrainData.alphamapResolution = alphamapResolution;
        terrainData.SetDetailResolution(detailResolution, detailResolutionPerPatch);

        terrainData.name = name + tileCoords.ToString();
        terrainData.terrainLayers = new TerrainLayer[3] { rockLayer, grassLayer, sandLayer };
        terrainData.SetAlphamaps(0, 0, CreateAlphaMap(terrainData, heights));

        return terrainData;
    }

    private float[,,] CreateAlphaMap(TerrainData terrainData, float[,] heights)
    {

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
                float height = WorldSpaceHeight(heights[x, y]);

                // Steepness is given as an angle, 0..90 degrees. Divide
                // by 90 to get an alpha blending value in the range 0..1.
                if (height < 0)
                {
                    map[x, y, 0] = 0;
                    map[x, y, 1] = 0;
                    map[x, y, 2] = 1;
                }
                else
                {
                    float frac = angle / 90.0f;
                    map[x, y, 0] = frac;
                    map[x, y, 1] = 1 - frac;
                }
            }
        }
        return map;
    }

    private List<Vector2Int> ChunkCoordsInRange(Vector3 worldPos, int range)
    {
        Vector2Int chunkPos = WorldToChunkCoords(new Vector2(worldPos.x, worldPos.z));
        List<Vector2Int> chunksInRange = new List<Vector2Int>();
        for (int x = -range + chunkPos.x; x <= range + chunkPos.x; x++)
        {
            for (int y = -range + chunkPos.y; y <= range + chunkPos.y; y++)
            {
                chunksInRange.Add(new Vector2Int(x, y));
            }
        }
        return chunksInRange;
    }

    private float WorldSpaceHeight(float height)
    {
        return height * heightScale + transform.position.y;
    }

    private Vector2Int WorldToChunkCoords(Vector2 worldPos)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPos.x / widthScale), Mathf.FloorToInt(worldPos.y / widthScale));
    }

}