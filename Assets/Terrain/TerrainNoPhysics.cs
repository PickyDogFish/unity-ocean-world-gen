using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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

    
    
    
    private Material rendererMaterial;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<MeshFilter>().sharedMesh = GridBuilder.BuildClipMap(32, 3);
        GetComponent<MeshFilter>().sharedMesh.name = "terrain_clipmap";
        GetComponent<MeshRenderer>().material = terrainMaterial;
    }

    void SetMaterialParameters(){
        terrainMaterial.SetVector("ClipMap_ViewerPosition", playerTransform.position);
        terrainMaterial.SetFloat("_displacement", displacement);
        terrainMaterial.SetFloat("_scale", scale);
        terrainMaterial.SetFloat("_verticalOffset", verticalOffset);
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
