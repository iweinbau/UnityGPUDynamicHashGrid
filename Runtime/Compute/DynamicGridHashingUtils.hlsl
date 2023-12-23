#ifndef __GRIDHASHINGUTILS_HLSL__
#define __GRIDHASHINGUTILS_HLSL__

#define UINT32_MAX 0xFFFFFFFF

// Still to include your compute shader where you want to process your data
//[numthreads(256, 1, 1)]
//void HashData(uint3 id : SV_DispatchThreadID)
//{
//		Hash your data here
//}

RWStructuredBuffer<uint> _IndexBuffer;
RWStructuredBuffer<uint> _CellIndexBuffer;
RWStructuredBuffer<uint> _OffsetsBuffer;

int3 CellIndex(float3 pos, float radius) {
	return floor(pos / radius);
}

uint GetFlatCellIndex(int3 cellIndex, uint tableSize)
{
	const uint p1 = 73856093; // some large primes
	const uint p2 = 19349663;
	const uint p3 = 83492791;
	int n = p1 * cellIndex.x ^ p2 * cellIndex.y ^ p3 * cellIndex.z;
	return n % tableSize;
}
#endif