#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

void MainLightShadows_float (float3 WorldPos, out float Shadows)
{
#ifdef SHADERGRAPH_PREVIEW // Draws to the ShaderGraph preview
        Shadows = 1;
#else // Draws to the scene & game views
        #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
                float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(WorldPos));
        #else
                float4 shadowCoords = TransformWorldToShadowCoord(WorldPos);
        #endif
                Shadows = MainLightShadow(shadowCoords, WorldPos, half4(1,1,1,1), _MainLightOcclusionProbes);
        #endif
}

#endif
