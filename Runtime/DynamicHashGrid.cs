using UnityEngine;

namespace GPUDynamicHashGrid
{
    public abstract class DynamicHashGrid : MonoBehaviour
    {
        [SerializeField]
        protected int m_size;

        protected ComputeShader m_hashGridCP;

        protected int ClearOffsetKernelID;
        protected int CalculateOffsetKernelID;
        protected int BitonicSortKernelID;

        protected ComputeBuffer indexBuffer;
        protected ComputeBuffer cellIndexBuffer;
        protected ComputeBuffer cellOffsetBuffer;

        public void InitHashGrid()
        {
            m_hashGridCP = Resources.Load<ComputeShader>("DynamicHashGrid");

            ClearOffsetKernelID = m_hashGridCP.FindKernel("ClearCellOffsets");
            CalculateOffsetKernelID = m_hashGridCP.FindKernel("CalculateCellOffsets");
            BitonicSortKernelID = m_hashGridCP.FindKernel("BitonicSort");
            m_hashGridCP.SetInt("size", (int)m_size);

            InitHashGridBuffers();
            InitDynamicHashGrid();
        }

        public void DispatchKernel(ComputeShader compute, int kernel, int sizeX)
        {
            uint x, y, z;
            compute.GetKernelThreadGroupSizes(kernel, out x, out y, out z);
            Vector3Int Vector3Int = new Vector3Int((int)x, (int)y, (int)z);
            int numGroupsX = Mathf.CeilToInt(sizeX / (float)Vector3Int.x);
            compute.Dispatch(kernel, numGroupsX, 1, 1);
        }

        public virtual void ReleaseBuffers()
        {
            if (indexBuffer != null)
                indexBuffer.Release();
            if (cellIndexBuffer != null)
                cellIndexBuffer.Release();
            if (cellOffsetBuffer != null)
                cellOffsetBuffer.Release();
        }

        public void ClearOffsetBuffer()
        {
            DispatchKernel(m_hashGridCP, ClearOffsetKernelID, m_size);
        }

        public void Sort()
        {
            for (var k = 2; k <= m_size; k <<= 1)
            {
                m_hashGridCP.SetInt("k", k);
                for (var j = k >> 1; j > 0; j >>= 1)
                {
                    m_hashGridCP.SetInt("j", j);
                    DispatchKernel(m_hashGridCP, BitonicSortKernelID, m_size);
                }
            }
        }

        public void CalculateOffsets()
        {
            DispatchKernel(m_hashGridCP, CalculateOffsetKernelID, m_size);
        }

        private void InitHashGridBuffers()
        {
            int BUFFER_SIZE = System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint));
            indexBuffer = new ComputeBuffer((int)m_size, BUFFER_SIZE);
            cellIndexBuffer = new ComputeBuffer((int)m_size, BUFFER_SIZE);
            cellOffsetBuffer = new ComputeBuffer((int)m_size, BUFFER_SIZE);

            uint[] indexArray = new uint[m_size];
            for (uint i = 0; i < indexArray.Length; i++) indexArray[i] = i;
            indexBuffer.SetData(indexArray);
        }

        private void InitDynamicHashGrid()
        {

            m_hashGridCP.SetBuffer(BitonicSortKernelID, "_IndexBuffer", indexBuffer);
            m_hashGridCP.SetBuffer(CalculateOffsetKernelID, "_IndexBuffer", indexBuffer);

            m_hashGridCP.SetBuffer(BitonicSortKernelID, "_CellIndexBuffer", cellIndexBuffer);
            m_hashGridCP.SetBuffer(CalculateOffsetKernelID, "_CellIndexBuffer", cellIndexBuffer);

            m_hashGridCP.SetBuffer(ClearOffsetKernelID, "_OffsetsBuffer", cellOffsetBuffer);
            m_hashGridCP.SetBuffer(CalculateOffsetKernelID, "_OffsetsBuffer", cellOffsetBuffer);
        }
    }
}
