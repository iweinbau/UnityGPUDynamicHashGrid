# Dynamic Hash Grid on GPU

## Uniform hashed Grid
This projects implements a dynamic hash grid on the GPU. Dynamic meaning that the grid size and dimensions are not given. This type of data representation is ideal for a fast fixed-readius search of particles [1]. 
Particle positions are mapped to cells using spatial hashing. Where after particels are sorted based on there cell. Doing so result in particles in the same cell beeing next to each other [2].

## Sorting
Bitonic Merge Sort is used to sort particles based on their cell index [3]. This algoritm has been implemented on the GPU using a compute shader.

## Example
![Hash grid example](https://github.com/iweinbau/UnityGPUDynamicHashGrid/blob/main/Img/HashGridExample.PNG?raw=true)

Image showing in blue the visited neigbor cell particles, in red the particles in the same cell and black all other particles. Note since we map an infinit space in finite memory some non neigbouring cells can be mapped to the same bucket (see blue region in the top right of the immage).

## How to use
To start using Hashed grids in your own project you need to implement the abstract `DynamicHashGrid` class. Next you can initialize by calling `InitHashGrid();`. 
Last step involves to include the `DynamicGridHashingUtils.hlsl` in your particle processing compute shader and implement a kernel for hasing your particle positions.

```c
// Dynamic hash grid
#include "../../Runtime/Compute/DynamicGridHashingUtils.hlsl"

// Example hashing function
[numthreads(256, 1, 1)]
void HashData(uint3 id : SV_DispatchThreadID)
{
    uint index = _IndexBuffer[id.x];
    float3 pos = dataBuffer[index].position;
    int3 cellIndex = CellIndex(pos, cellSize);
    uint flatCellIndex = GetFlatCellIndex(cellIndex, dataCount);
    _CellIndexBuffer[index] = flatCellIndex;
}
```

Now you are ready to start using hashing grids, before you dispatch your procesing kernels you need to sort call the reset and sort functions. See code below to see an example update step
```c#
private void Update()
{
    dataProcessingCS.SetInt("dataCount", m_size);
    dataProcessingCS.SetFloat("cellSize", cellSize);
    dataProcessingCS.SetInt("target", (int)target);

    // Clear Offset buffer and hash particles
    ClearOffsetBuffer();
    DispatchKernel(dataProcessingCS, hashDataKernelID, m_size);

    // Sort and calculate offset
    Sort();
    CalculateOffsets();

    // Process particles
    DispatchKernel(dataProcessingCS, mainKernelID, m_size);
}
```

To efficiently iterate over the neigbour cells in your proccesing kernels
```c
int3 currentCellIndex = CellIndex(dataBuffer[index].position, cellSize);
for (int i = -1; i <= 1; ++i)
{
    for (int j = -1; j <= 1; ++j)
    {
        for (int k = -1; k <= 1; ++k)
        {
            int3 neighborCellIndex = currentCellIndex + int3(i, j, k);
            uint flatNeighborCellIndex = GetFlatCellIndex(neighborCellIndex, dataCount);
            uint neighborIterator = _OffsetsBuffer[flatNeighborCellIndex];

            while (neighborIterator < dataCount)
            {
                uint otherIndex = _IndexBuffer[neighborIterator];
                neighborIterator++;  // iterate...

                if (_CellIndexBuffer[otherIndex] != flatNeighborCellIndex) break;

                // Do something with neigboring particles
          }
    }
}
```

## Installing
To install the package in unity you can import it using the git url `https://github.com/iweinbau/UnityGPUDynamicHashGrid.git`

[[1] Fast Fixed-Radius Nearest Neighbor Search on the GPU](https://on-demand.gputechconf.com/gtc/2014/presentations/S4117-fast-fixed-radius-nearest-neighbor-gpu.pdf)

[[2] Particle-based Fluid Simulation based Fluid Simulation](https://developer.download.nvidia.com/presentations/2008/GDC/GDC08_ParticleFluids.pdf)

[[3] chapter-46-improved-gpu-sorting](https://developer.nvidia.com/gpugems/gpugems2/part-vi-simulation-and-numerical-algorithms/chapter-46-improved-gpu-sorting)
