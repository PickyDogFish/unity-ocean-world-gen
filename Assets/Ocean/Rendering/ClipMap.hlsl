float3 ClipMap_ViewerPosition;

float3 ClipMapVertex(float3 positionOS)
{
    float3 worldPos = float3(ClipMap_ViewerPosition.x + positionOS.x, 0, ClipMap_ViewerPosition.z + positionOS.z);

	return worldPos;
}