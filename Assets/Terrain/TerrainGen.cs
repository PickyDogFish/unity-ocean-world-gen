
using System.Collections.Generic;
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
    [SerializeField] private int tileRange = 1;

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
        //AddTerrain(Vector2Int.zero);
        //AddTerrain(new Vector2Int(0,1));
        //AddTerrain(new Vector2Int(0,-1));

        PrintNeighbors(Vector2Int.zero);
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
        PrintNeighbors(terrainCoords);
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