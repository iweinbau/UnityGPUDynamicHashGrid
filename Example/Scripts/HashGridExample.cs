using UnityEngine;
using Random = UnityEngine.Random;
using GPUDynamicHashGrid;

[RequireComponent(typeof(DataPointVisualization))]
public class HashGridExample : DynamicHashGrid
{
    [Min(0.1f)]
    public float cellSize;
    public uint target;

    public ComputeShader dataProcessingCS;
    private int mainKernelID;
    private int hashDataKernelID;

    DataPoint[] dataPoints;
    ComputeBuffer dataBuffer;
    struct DataPoint
    {
        public Vector3 position;
        public Vector3 color;
    }

    private void Start()
    {
        SpanwparticleBuffer();
        InitHashGrid();
        InitKernels();
        InitBuffers();
        BindBuffers();
        GetComponent<DataPointVisualization>().Init(ref dataBuffer);
    }

    private void Update()
    {
        dataProcessingCS.SetInt("dataCount", m_size);
        dataProcessingCS.SetFloat("cellSize", cellSize);
        dataProcessingCS.SetInt("target", (int)target);

        ClearOffsetBuffer();
        DispatchKernel(dataProcessingCS, hashDataKernelID, m_size);

        Sort();
        CalculateOffsets();
        DispatchKernel(dataProcessingCS, mainKernelID, m_size);
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
    }

    private void BindBuffers()
    {
        dataProcessingCS.SetBuffer(mainKernelID, "_IndexBuffer", indexBuffer);
        dataProcessingCS.SetBuffer(mainKernelID, "_CellIndexBuffer", cellIndexBuffer);
        dataProcessingCS.SetBuffer(mainKernelID, "_OffsetsBuffer", cellOffsetBuffer);
        dataProcessingCS.SetBuffer(mainKernelID, "dataBuffer", dataBuffer);

        dataProcessingCS.SetBuffer(hashDataKernelID, "_IndexBuffer", indexBuffer);
        dataProcessingCS.SetBuffer(hashDataKernelID, "_CellIndexBuffer", cellIndexBuffer);
        dataProcessingCS.SetBuffer(hashDataKernelID, "_OffsetsBuffer", cellOffsetBuffer);
        dataProcessingCS.SetBuffer(hashDataKernelID, "dataBuffer", dataBuffer);
    }

    private void InitKernels()
    {
        mainKernelID = dataProcessingCS.FindKernel("CSMain");
        hashDataKernelID = dataProcessingCS.FindKernel("HashData");
    }

    private void InitBuffers()
    {
        int BUFFER_SIZE = System.Runtime.InteropServices.Marshal.SizeOf(typeof(DataPoint));
        dataBuffer = new ComputeBuffer((int)m_size, BUFFER_SIZE);
        dataBuffer.SetData(dataPoints);
    }

    public void SpanwparticleBuffer()
    {
        dataPoints = new DataPoint[m_size];

        for (int i = 0; i < m_size; i++)
        {
            float randomX = Random.Range(-0.5f, 0.5f);
            float randomY = Random.Range(-0.5f, 0.5f);
            float randomZ = Random.Range(-0.5f, 0.5f);

            Vector3 pos = new Vector3(randomX, randomY, randomZ);
            DataPoint p = new DataPoint();
            p.position = transform.TransformPoint(pos);
            p.color = Vector3.zero;
            dataPoints[i] = p;
        }
    }

    public override void ReleaseBuffers()
    {
        base.ReleaseBuffers();
        if (dataBuffer != null)
            dataBuffer.Release();
    }

    private void OnValidate()
    {
        cellSize = Mathf.Max(cellSize, 0.0001f);
    }

    private void OnDrawGizmos()
    {
        // Draw Bounds
        var m = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1, 0, 1, 0.5f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.matrix = m;

        DrawGrid();
    }

    void DrawGrid()
    {
        Gizmos.color = new Color(0.5f,0.5f,0.5f,0.5f);
        Vector3 gridSize = transform.localScale / Mathf.Max(cellSize, 0.0001f);

        Vector3 offset = new Vector3(gridSize.x * 0.5f * cellSize, gridSize.y * 0.5f * cellSize, gridSize.z * 0.5f * cellSize);
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    Vector3 cellPosition = new Vector3(x * cellSize, y * cellSize, z * cellSize) - offset + transform.position;
                    DrawCell(cellPosition);
                }
            }
        }
    }

    void DrawCell(Vector3 cellPosition)
    {
        Gizmos.DrawWireCube(cellPosition + new Vector3(0.5f, 0.5f, 0.5f) * cellSize, new Vector3(cellSize, cellSize, cellSize));
    }
}
