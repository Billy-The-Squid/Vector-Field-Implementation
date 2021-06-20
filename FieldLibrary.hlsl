interface FieldInterface
{
    float3 Field(float3 position);
};

// class Coulomb : FieldInterface {
// 	float3 Field(float3 position) {
// 		for (int i = 0; i < _NumberOfCharges; i++)
// 		{
// 			float3 displacement = position - _ChargePositions[i];
// 			float distance = sqrt(displacement.x * displacement.x + displacement.y * displacement.y + displacement.z * displacement.z);
// 			return _CoulombConstant * _Charges[i] * displacement / (pow(distance, 3));
// 		}
// 	}
// }

class Outwards : FieldInterface
{
    float3 Field(float3 position)
    {
        return position;
    }
};

class Swirl : FieldInterface
{
    float3 Field(float3 position)
    {
        float3 val;
        val.x = -position.z;
        val.y = 0;
        val.z = position.x;
        return val;
    }
};

// Every type that's added must also be present in the enum in VectorFields.cs

