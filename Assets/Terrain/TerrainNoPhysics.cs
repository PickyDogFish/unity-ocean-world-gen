using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TerrainNoPhysics : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private Transform playerTransform;

    [Header("Terrain settings")]
    [SerializeField] private float displacement = 10;
    [SerializeField] private float scale = 100;
    [SerializeField] private float verticalOffset = -0.5f;

    //Number of noise octaves that are included in normal calculation 
    [SerializeField] private int normalOctaves = 6;

    [SerializeField] private Texture2D sand;
    [SerializeField] private Texture2D grass;
    [SerializeField] private Texture2D rock;
    [SerializeField] private Texture2D snow;
    private Texture2DArray groundTextures;
    
    
    private Material rendererMaterial;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<MeshFilter>().sharedMesh = GridBuilder.BuildClipMap(64, 4);
        GetComponent<MeshFilter>().sharedMesh.name = "terrain_clipmap";
        GetComponent<MeshRenderer>().material = terrainMaterial;
        groundTextures = new Texture2DArray(512,512,4, TextureFormat.RGBA32, false);
        groundTextures.wrapMode = TextureWrapMode.Repeat;
        groundTextures.filterMode = FilterMode.Trilinear;
        groundTextures.SetPixels(sand.GetPixels(), 0);
        groundTextures.SetPixels(grass.GetPixels(), 1);
        groundTextures.SetPixels(rock.GetPixels(), 2);
        groundTextures.SetPixels(snow.GetPixels(), 3);
        groundTextures.Apply();
    }

    void SetMaterialParameters(){
        terrainMaterial.SetVector("ClipMap_ViewerPosition", playerTransform.position);
        terrainMaterial.SetFloat("_displacement", displacement);
        terrainMaterial.SetFloat("_scale", scale);
        terrainMaterial.SetFloat("_verticalOffset", verticalOffset);
        terrainMaterial.SetFloat("_percentUnderwater", Mathf.Abs(verticalOffset));
        terrainMaterial.SetTexture("_groundTextures", groundTextures);
        terrainMaterial.SetFloat("_normalNoiseOctaves", normalOctaves);
    }

    // Update is called once per frame
    void Update()
    {
        if ((GetComponent<MeshFilter>().sharedMesh.bounds.center - playerTransform.position).magnitude > 10){
            GetComponent<MeshFilter>().sharedMesh.bounds = new Bounds(new Vector3(playerTransform.position.x, 0, playerTransform.position.z),  GetComponent<MeshFilter>().sharedMesh.bounds.size);
        }

        SetMaterialParameters();
    }
}
