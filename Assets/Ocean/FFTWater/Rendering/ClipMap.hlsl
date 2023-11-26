//simplified version of https://github.com/gasgiant/Ocean-URP
float3 ClipMap_ViewerPosition;

float3 ClipMapVertex(float3 positionOS)
{
	float3 snappedViewerPos = float3(floor(ClipMap_ViewerPosition.x), 0, floor(ClipMap_ViewerPosition.z));
    float3 worldPos = float3(snappedViewerPos.x + positionOS.x, 0, snappedViewerPos.z + positionOS.z);

	return worldPos;
}