// This is our vertex shader

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	StructuredBuffer<float3> _Positions;
	StructuredBuffer<float3> _Vectors; // Is not filled by default VectorField
	StructuredBuffer<float3> _PlotVectors;
	StructuredBuffer<float3> _Vectors2;
	StructuredBuffer<float3> _Vectors3;
	StructuredBuffer<float> _Magnitudes;
#endif
// Why is this check necessary?

void ConfigureProcedural () 
{
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		float4x4 transformation = 0.0; 
		transformation._m33 = 1.0;
		float3 position = _Positions[unity_InstanceID];
		float3 vect = _PlotVectors[unity_InstanceID];
		// Where'd this index come from?
		float3 vect_2 = _Vectors2[unity_InstanceID];
		float3 vect_3 = _Vectors3[unity_InstanceID];

		// The position is, well, the position. 
		transformation._m03_m13_m23 = position;
		// The (0,1,0) direction should always map to where the vector is pointing. (This depends on the mesh though)
		transformation._m02_m12_m22 = vect;
		// Putting these back in the transformation matrix
		transformation._m01_m11_m21 = vect_2;
		transformation._m00_m10_m20 = -vect_3;
		// Do I need to switch the order of these?
	
		// And exporting it. 
		unity_ObjectToWorld = transformation;
	#endif
}