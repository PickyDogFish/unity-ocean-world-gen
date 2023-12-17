//simplified version of https://github.com/gasgiant/Ocean-URP
float3 ClipMap_ViewerPosition;

float3 ClipMapVertex(float3 positionOS)
{
	float tileSize = 1; 
	float3 snappedViewerPos = float3(floor(ClipMap_ViewerPosition.x / tileSize) * tileSize, 0, floor(ClipMap_ViewerPosition.z / tileSize) * tileSize);
    float3 worldPos = float3(snappedViewerPos.x + positionOS.x, 0, snappedViewerPos.z + positionOS.z);

	return worldPos;
}