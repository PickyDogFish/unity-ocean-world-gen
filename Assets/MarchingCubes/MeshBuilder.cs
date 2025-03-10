using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.Rendering;

namespace MarchingCubes {

struct VertexData
{
    public Vector3 position;
    public Vector3 normal;
}

//
// Isosurface mesh builder with the marching cubes algorithm
//
sealed class MeshBuilder : System.IDisposable
{
    #region Public members

    public Mesh Mesh => _mesh;

    public MeshBuilder(int x, int y, int z, int budget, ComputeShader compute)
      => Initialize((x, y, z), budget, compute);

    public MeshBuilder(Vector3Int dims, int budget, ComputeShader compute)
      => Initialize((dims.x, dims.y, dims.z), budget, compute);

    public void Dispose()
      => ReleaseAll();

    public void BuildIsosurface(ComputeBuffer voxels, float target, float scale)
      => RunCompute(voxels, target, scale);

    public void BuildIsoSurface(RenderTexture voxels, float target, float scale){
      RunCompute(voxels, target, scale);
    }

    #endregion

    #region Private members

    (int x, int y, int z) _grids;
    int _triangleBudget;
    ComputeShader _compute;

    void Initialize((int, int, int) dims, int budget, ComputeShader compute)
    {
        _grids = dims;
        _triangleBudget = budget;
        _compute = compute;

        AllocateBuffers();
        AllocateMesh(3 * _triangleBudget);
    }

    void ReleaseAll()
    {
        ReleaseBuffers();
        ReleaseMeshBuffers();
    }

    void RunCompute(ComputeBuffer voxels, float target, float scale)
    {
        _counterBuffer.SetCounterValue(0);

        // Isosurface reconstruction
        _compute.SetInts("Dims", _grids);
        _compute.SetInt("MaxTriangle", _triangleBudget);
        _compute.SetFloat("Scale", scale);
        _compute.SetFloat("Isovalue", target);
        _compute.SetBuffer(0, "TriangleTable", _triangleTable);
        RenderTexture temp = new RenderTexture(8,8,0);
        temp.dimension = TextureDimension.Tex3D;
        temp.Create();
        _compute.SetTexture(0, "VoxelsTex", temp);
        _compute.SetBool("Use3dTex", false);
        _compute.SetBuffer(0, "Voxels", voxels);
        _compute.SetBuffer(0, "VertexBuffer", _vertexBuffer);
        _compute.SetBuffer(0, "IndexBuffer", _indexBuffer);
        _compute.SetBuffer(0, "Counter", _counterBuffer);
        _compute.DispatchThreads(0, _grids);

        // Clear unused area of the buffers.
        _compute.SetBuffer(1, "VertexBuffer", _vertexBuffer);
        _compute.SetBuffer(1, "IndexBuffer", _indexBuffer);
        _compute.SetBuffer(1, "Counter", _counterBuffer);
        _compute.DispatchThreads(1, 1024, 1, 1);

        // Bounding box
        var ext = new Vector3(_grids.x, _grids.y, _grids.z) * scale;
        _mesh.bounds = new Bounds(Vector3.zero, ext);

        // Retrieve vertex data
        VertexData[] vertices = new VertexData[_triangleBudget * 3];
        _vertexBuffer.GetData(vertices);
        // Extract positions into a separate array
        Vector3[] positions = new Vector3[vertices.Length];
        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            positions[i] = vertices[i].position;
            normals[i] = vertices[i].normal;
        }


        // Retrieve index data
        int[] indices = new int[_triangleBudget * 3];
        _indexBuffer.GetData(indices);

        // Assign data to the mesh
        _mesh.Clear();
        _mesh.vertices = positions;
        _mesh.triangles = indices;
        _mesh.normals = normals;


        _mesh.UploadMeshData(true);
        temp.Release();
    }
    void RunCompute(RenderTexture voxels, float target, float scale)
    {
        _counterBuffer.SetCounterValue(0);

        // Isosurface reconstruction
        _compute.SetInts("Dims", _grids);
        _compute.SetInt("MaxTriangle", _triangleBudget);
        _compute.SetFloat("Scale", scale);
        _compute.SetFloat("Isovalue", target);
        _compute.SetBuffer(0, "TriangleTable", _triangleTable);
        _compute.SetBool("Use3dTex", true);
        _compute.SetTexture(0, "VoxelsTex", voxels);
        ComputeBuffer temp = new ComputeBuffer(16,4);
        _compute.SetBuffer(0, "Voxels", temp);
        _compute.SetBuffer(0, "VertexBuffer", _vertexBuffer);
        _compute.SetBuffer(0, "IndexBuffer", _indexBuffer);
        _compute.SetBuffer(0, "Counter", _counterBuffer);
        _compute.DispatchThreads(0, _grids);

        // Clear unused area of the buffers.
        _compute.SetBuffer(1, "VertexBuffer", _vertexBuffer);
        _compute.SetBuffer(1, "IndexBuffer", _indexBuffer);
        _compute.SetBuffer(1, "Counter", _counterBuffer);
        _compute.DispatchThreads(1, 1024, 1, 1);

        // Bounding box
        var ext = new Vector3(_grids.x, _grids.y, _grids.z) * scale;
        _mesh.bounds = new Bounds(Vector3.zero, ext);
        _mesh.UploadMeshData(false);
        temp.Dispose();
    }



    #endregion

    #region Compute buffer objects

    ComputeBuffer _triangleTable;
    ComputeBuffer _counterBuffer;

    void AllocateBuffers()
    {
        // Marching cubes triangle table
        _triangleTable = new ComputeBuffer(256, sizeof(ulong));
        _triangleTable.SetData(PrecalculatedData.TriangleTable);

        // Buffer for triangle counting
        _counterBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Counter);
    }

    void ReleaseBuffers()
    {
        _triangleTable.Dispose();
        _counterBuffer.Dispose();
    }

    #endregion

    #region Mesh objects

    Mesh _mesh;
    GraphicsBuffer _vertexBuffer;
    GraphicsBuffer _indexBuffer;

    void AllocateMesh(int vertexCount)
    {
        _mesh = new Mesh();

        // We want GraphicsBuffer access as Raw (ByteAddress) buffers.
        _mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

        // Vertex position: float32 x 3
        var vp = new VertexAttributeDescriptor
          (VertexAttribute.Position, VertexAttributeFormat.Float32, 3);

        // Vertex normal: float32 x 3
        var vn = new VertexAttributeDescriptor
          (VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);

        // Vertex/index buffer formats
        _mesh.SetVertexBufferParams(vertexCount, vp, vn);
        _mesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt32);

        // Submesh initialization
        _mesh.SetSubMesh(0, new SubMeshDescriptor(0, vertexCount),
                         MeshUpdateFlags.DontRecalculateBounds);

        // GraphicsBuffer references
        _vertexBuffer = _mesh.GetVertexBuffer(0);
        _indexBuffer = _mesh.GetIndexBuffer();
    }

    void ReleaseMeshBuffers()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }

    #endregion
}

} // namespace MarchingCubes
