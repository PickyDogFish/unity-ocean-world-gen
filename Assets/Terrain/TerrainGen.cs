
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class TerrainGen : MonoBehaviour
{
    private ComputeShader noiseCS;


    [Header("Overall settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] public int tileRange = 1;
    [Range(0, 1)][SerializeField] private float percentUnderwater = 0.6f;

    [Header("Noise settings")]
    [SerializeField] private float noiseScale = 1;
    
    [Header("Chunk settings")]
    [SerializeField] private Vector2Int noiseChunkOffset = Vector2Int.zero; 
    [SerializeField] private int widthScale = 64;
    [SerializeField] private int heightScale = 64;
    private Vector3 size { get { return new Vector3(widthScale, heightScale, widthScale); } }



    [Header("Texture settings")]
    [SerializeField] TerrainData templateTerrain;

    private int heightmapResolution { get { return widthScale + 1; } } //apparently has to be one more
    private int detailResolution { get { return widthScale; } }
    private int detailResolutionPerPatch = 8;
    private int alphamapResolution { get { return widthScale + 1; } }
    private int baseTextureResolution { get { return widthScale/2; } }



    private Dictionary<Vector2Int, Terrain> generatedTileDictionary = new Dictionary<Vector2Int, Terrain>();
    private Dictionary<Vector2Int, Terrain> shownTileDictionary = new Dictionary<Vector2Int, Terrain>();

    public void InitializeTerrainGen(){
        if (noiseCS == null){
            noiseCS = Resources.Load<ComputeShader>("NoiseGenerator");
        }
        transform.position = new Vector3(0, -heightScale * percentUnderwater, 0);
    }
    void Start()
    {
        RemoveAllTerrain();
        InitializeTerrainGen();

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

    public RenderTexture PreviewNoise(){
        return NoiseGen.GetNoiseRT(new Vector2Int(-tileRange, -tileRange) + noiseChunkOffset, noiseCS, heightmapResolution, (tileRange * 2 + 1) * (heightmapResolution-1), noiseScale);
    }

    void Update()
    {
        GenerateAndShowNearbyTerrain();
        HideFarTerrain();
    }
    private void PrintNeighbors(Vector2Int terrainCoords)
    {
        Debug.Log(terrainCoords + "    left: " + generatedTileDictionary[terrainCoords].leftNeighbor + "    top: " + generatedTileDictionary[terrainCoords].topNeighbor + "    right: " + generatedTileDictionary[terrainCoords].rightNeighbor + "    bottom: " + generatedTileDictionary[terrainCoords].bottomNeighbor);
    }

    public void GenerateAndShowNearbyTerrain(){
        List<Vector2Int> tilesInRange = ChunkCoordsInRange(cameraTransform.position, tileRange);
        foreach (Vector2Int newChunk in tilesInRange)
        {
            ShowTerrain(newChunk);
        }
    }

    public void HideFarTerrain(){
        List<Vector2Int> tilesInRange = ChunkCoordsInRange(cameraTransform.position, tileRange);
        List<Vector2Int> chunksToHide = shownTileDictionary.Keys.Except(tilesInRange).ToList();
        foreach (Vector2Int outOfRangeTile in chunksToHide)
        {
            HideTerrain(outOfRangeTile);
        }
    }

    /// <summary>
    /// Sets terrain tile to active if already generated, otherwise generates new terrain tile. Handles dictionaries
    /// </summary>
    /// <param name="terrainCoords"></param>
    public void ShowTerrain(Vector2Int terrainCoords)
    {
        if (generatedTileDictionary.ContainsKey(terrainCoords))
        {
            Terrain tile = generatedTileDictionary[terrainCoords];
            tile.gameObject.SetActive(true);
            shownTileDictionary.TryAdd(terrainCoords, tile);
        }
        else
        {
            AddTerrain(terrainCoords);
        }
    }

    /// <summary>
    /// Sets terrain tile active to false and handles dictionaries
    /// </summary>
    /// <param name="terrainCoords"></param>
    public void HideTerrain(Vector2Int terrainCoords)
    {
        if (shownTileDictionary.ContainsKey(terrainCoords))
        {
            Terrain tile = shownTileDictionary[terrainCoords];
            tile.gameObject.SetActive(false);
            shownTileDictionary.Remove(terrainCoords);
        }
    }

    /// <summary>
    /// Generates a terrain tile, sets the terrain neighbors and adds it to generated and shown dictionaries 
    /// </summary>
    /// <param name="terrainCoords"></param>
    public void AddTerrain(Vector2Int terrainCoords)
    {
        Terrain terrain = CreateTerrainTile(terrainCoords);
        terrain.gameObject.hideFlags = HideFlags.HideInHierarchy;
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

    public void RemoveAllTerrain(){
        shownTileDictionary.Clear();
        generatedTileDictionary.Clear();
        while (transform.childCount > 0){
            DestroyImmediate(transform.GetChild(0).gameObject);
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

        terrainData.name = name + tileCoords.ToString();
        terrainData.size = new Vector3(size.x/widthScale*32, size.y, size.z/widthScale*32);

        terrainData.baseMapResolution = baseTextureResolution;
        terrainData.heightmapResolution = heightmapResolution;
        //float[,] heights = NoiseGen.GetNoiseArray(tileCoords + noiseChunkOffset, noiseCS, heightmapResolution, noiseScale);
        //RenderTexture rt = NoiseGen.GetNoiseRT(tileCoords + noiseChunkOffset, noiseCS, heightmapResolution, heightmapResolution, noiseScale);
        NoiseGen.TerrainGenData terrainGenData = NoiseGen.GetTerrainRT(tileCoords + noiseChunkOffset, noiseCS, heightmapResolution, heightmapResolution, noiseScale);
        Graphics.SetRenderTarget(terrainGenData.heightMap);
        terrainData.CopyActiveRenderTextureToHeightmap(new RectInt(0,0, heightmapResolution, heightmapResolution), Vector2Int.zero, TerrainHeightmapSyncControl.HeightAndLod);
        //terrainData.SetHeights(0, 0, heights);

        terrainData.alphamapResolution = alphamapResolution;
        terrainData.SetDetailResolution(detailResolution, detailResolutionPerPatch);

        terrainData.terrainLayers = templateTerrain.terrainLayers;
        Graphics.SetRenderTarget(terrainGenData.splatMap);
        terrainData.CopyActiveRenderTextureToTexture(TerrainData.AlphamapTextureName, 0, new RectInt(0, 0, terrainData.alphamapResolution, terrainData.alphamapResolution), Vector2Int.zero, false);
        //terrainData.SetAlphamaps(0, 0, CreateTextureAlphaMap(terrainData, heights));


        terrainData.detailPrototypes = templateTerrain.detailPrototypes;
        terrainData.SetDetailScatterMode(DetailScatterMode.CoverageMode);
        int[,] detailMap = terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, 0);
        //detailMap = CreateDetailAlphaMap(detailMap, heights);
        //terrainData.SetDetailLayer(0, 0, 0, detailMap);
        //terrainData.SetDetailLayer(0, 0, 1, detailMap);
        //terrainData.SetDetailLayer(0, 0, 2, detailMap);
        //terrainData.SetDetailLayer(0, 0, 3, detailMap);
        return terrainData;
    }


    private int[,] CreateDetailAlphaMap(int[,] detailMap, float[,] heights)
    {
        // For each point on the alphamap...
        for (int y = 0; y < detailMap.GetLength(1); y++)
        {
            for (int x = 0; x < detailMap.GetLength(0); x++)
            {
                float height = WorldSpaceHeight(heights[x, y]);
                if (height > 0)
                {
                    detailMap[x, y] = 0;
                }
            }
        }
        return detailMap;
    }

    private float[,,] CreateTextureAlphaMap(TerrainData terrainData, float[,] heights)
    {

        float[,,] map = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, 4];

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

    /// <summary>
    /// Returns a list of chunks in range of worldPos
    /// </summary>
    /// <param name="worldPos"></param>
    /// <param name="range">Range in chunks</param>
    /// <returns></returns>
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

    /// <summary>
    /// Transforms the 0-1 heightmap value to world space height
    /// </summary>
    /// <param name="height">Heightmap value</param>
    /// <returns>World Space height</returns>
    private float WorldSpaceHeight(float height)
    {
        return height * heightScale + transform.position.y;
    }

    private Vector2Int WorldToChunkCoords(Vector2 worldPos)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPos.x / widthScale), Mathf.FloorToInt(worldPos.y / widthScale));
    }

}