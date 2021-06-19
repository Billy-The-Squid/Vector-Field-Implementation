// This is our fragment shader.

Shader "Vectors/Shader"
{
	Properties
	{
		// _Scaling ("Scaling Factor", float) = 0.1
	} // This needs to be used somewhere. Where?

	SubShader
	{
		CGPROGRAM

		// Renders the surface. Requires a ConfigureSurface function.
		#pragma surface ConfigureSurface Standard fullforwardshadows addshadow
		// Does instancing, including(?) placing points. Requires a ConfigureProcedural function.
		#pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
		#pragma editor_sync_compilation
		#pragma target 4.5

		// This is where the work of calculating transformations is done. 
		#include "PointsPlot.hlsl"

		struct Input
		{
			float3 worldPos;
		};

		// #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		// 	float2 FindMaxMinMagnitudes() {
		// 		float max = _Magnitudes[0];
		// 		float min = _Magnitudes[0];
		// 		for(int i = 1; i < _Magnitudes.Length; i++) {
		// 			float mag = _Magnitudes[i];
		// 			if(mag < min) {
		// 				min = mag;
		// 			}
		// 			if (mag > max) {
		// 				max = mag;
		// 			}
		// 		}
		// 		return float2(max, min);
		// 	}
		// #endif

		float3 HUEtoRGB(in float H)
		{ // Borrowed this from the internet: https://www.chilliant.com/rgb2hsv.html
		float R = abs(H * 6 - 3) - 1;
		float G = 2 - abs(H * 6 - 2);
		float B = 2 - abs(H * 6 - 4);
		return saturate(float3(R,G,B));
		}

		#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
			void ConfigureSurface(Input input, inout SurfaceOutputStandard surface) {
				float vect_mag = _Magnitudes[unity_InstanceID];
				float3 output = HUEtoRGB(vect_mag);
				surface.Albedo = output;
			}
		#else
			void ConfigureSurface (Input input, inout SurfaceOutputStandard surface)
			{
				surface.Albedo = saturate(unity_ObjectToWorld._m02_m12_m22 * 0.5 + 0.5);
			}
		#endif


		ENDCG
	}

	FallBack "Diffuse"
}
