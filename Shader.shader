// This is our fragment shader.

Shader "Vectors/Shader"
{
	Properties
	{
		_Scaling ("Scaling Factor", float) = 0.1
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

		// This is temporary! %%%%%%%%%% 
		struct Input
		{
			float3 worldPos;
		};

		void ConfigureSurface (Input input, inout SurfaceOutputStandard surface)
		{
			surface.Albedo = saturate(unity_ObjectToWorld._m02_m12_m22 * 0.5 + 0.5);
		}


		ENDCG
	}

	FallBack "Diffuse"
}
