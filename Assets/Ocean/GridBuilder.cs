using UnityEngine;

public class GridBuilder : MonoBehaviour
{
    [SerializeField] private int size = -1;
    [SerializeField] private float scale = 1;

    private void Start()
    {
        Debug.Log("setting mesh");
        //TODO check if MeshFilter component exists
        Debug.Assert(GetComponent<MeshFilter>() != null);
        GetComponent<MeshFilter>().mesh = BuildPlane(size, size, Vector3.zero, scale);
        //GetComponentInChildren<MeshFilter>().mesh = BuildRing(128);
        //GetComponentInChildren<MeshFilter>().mesh = BuildClipMap(16, 3);

    }


    //amount of overlap between the rings
    private const int Overlap = 2;

    private static class GlobalShaderVariables
    {
        public static readonly int ViewerPosition = Shader.PropertyToID("ClipMap_ViewerPosition");
        public static readonly int Scale = Shader.PropertyToID("ClipMap_Scale");
        public static readonly int LevelHalfSize = Shader.PropertyToID("ClipMap_LevelHalfSize");
    }

    public static int ClipLevelHalfSize(int vertexDensity) => (vertexDensity + 1) * 4 - 1;


    public static Mesh BuildClipMap(int vertexDensity, int clipMapLevels)
    {
        int clipLevelHalfSize = ClipLevelHalfSize(vertexDensity);

        Mesh mesh = new Mesh();
        mesh.name = "clipmap";
        CombineInstance[] combine = new CombineInstance[clipMapLevels + 1];

        //the middle plane
        combine[0].mesh = BuildPlane(clipLevelHalfSize, clipLevelHalfSize, new Vector3(1,0,1) * (clipLevelHalfSize+1)/2);
        combine[0].transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);


        //the rings
        Mesh ring = BuildRing(clipLevelHalfSize);
        for (int i = 0; i < clipMapLevels; i++)
        {
            combine[i + 1].mesh = ring;
            combine[i + 1].transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * Mathf.Pow(2, i));
        }

        //combine[1].mesh = BuildPlane(clipLevelHalfSize, clipLevelHalfSize, Vector3.zero);
        //combine[1].transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

        mesh.CombineMeshes(combine, true);
        return mesh;
    }

    private static Mesh BuildRing(int clipLevelHalfSize)
    {
        Mesh mesh = new Mesh();
        mesh.name = "clipmap ring";

        int shortSide = (clipLevelHalfSize + 1) / 2;
        int longSide = clipLevelHalfSize + 1;

        CombineInstance[] combine = new CombineInstance[8];

        Vector3 pivot = (Vector3.right + Vector3.forward) * (longSide + 1);
        //bottom left
        combine[0].mesh = BuildPlane(shortSide, shortSide, pivot);
        combine[0].transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

        //middle left
        combine[1].mesh = BuildPlane(shortSide, longSide, pivot);
        combine[1].transform = Matrix4x4.TRS(Vector3.forward * shortSide, Quaternion.identity, Vector3.one);

        //top left
        combine[2].mesh = BuildPlane(shortSide, shortSide, pivot);
        combine[2].transform = Matrix4x4.TRS(Vector3.forward * (shortSide + longSide), Quaternion.identity, Vector3.one);

        //bottom middle
        combine[3].mesh = BuildPlane(longSide, shortSide, pivot);
        combine[3].transform = Matrix4x4.TRS(Vector3.right * shortSide, Quaternion.identity, Vector3.one);

        //bottom right
        combine[4].mesh = BuildPlane(shortSide, shortSide, pivot);
        combine[4].transform = Matrix4x4.TRS(Vector3.right * (shortSide + longSide), Quaternion.identity, Vector3.one);

        //middle right
        combine[5].mesh = BuildPlane(shortSide, longSide, pivot);
        combine[5].transform = Matrix4x4.TRS(Vector3.right * (shortSide + longSide) + Vector3.forward * shortSide, Quaternion.identity, Vector3.one);

        //top middle
        combine[6].mesh = BuildPlane(longSide, shortSide, pivot);
        combine[6].transform = Matrix4x4.TRS(Vector3.right * shortSide + Vector3.forward * (shortSide + longSide), Quaternion.identity, Vector3.one);

        //top right
        combine[7].mesh = BuildPlane(shortSide, shortSide, pivot);
        combine[7].transform = Matrix4x4.TRS((Vector3.right + Vector3.forward) * (shortSide + longSide), Quaternion.identity, Vector3.one);

        mesh.CombineMeshes(combine, true);
        return mesh;
    }

    public static Mesh BuildPlane(int width, int height, Vector3 pivot, float scale = 1)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Clipmap plane";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        Vector3[] verts = new Vector3[(width + 1) * (height + 1)];
        Vector2[] uvs = new Vector2[(width + 1) * (height + 1)];
        int[] indices = new int[width * height * 2 * 3];
        Vector3[] normals = new Vector3[(width + 1) * (height + 1)];

        for (int z = 0; z < height + 1; z++)
        {
            for (int x = 0; x < width + 1; x++)
            {
                Vector3 normalPosition = new Vector3(x * scale, 1, z * scale);
                verts[x + z * (width + 1)] = normalPosition - pivot;
                normals[x + z * (width + 1)] = Vector3.up;
                uvs[x + z * (width + 1)] = new Vector2((float)x/width, (float)z/width);
            }
        }

        int tris = 0;
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = x + z * (width + 1);
                indices[tris++] = index;
                indices[tris++] = index + width + 1;
                indices[tris++] = index + width + 2;

                indices[tris++] = index;
                indices[tris++] = index + width + 2;
                indices[tris++] = index + 1;
            }
        }

        mesh.vertices = verts;
        mesh.triangles = indices;
        mesh.uv = uvs;
        mesh.normals = normals;

        return mesh;
    }

}
