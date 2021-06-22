//interface FieldInterface
//{
//    float3 Field(float3 position);
//};

// %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// To add a new field to the Field Library, follow these steps:
//   1) Create a new class in the field library file, named after that function, 
//      that inherits from FieldInterface.
//   2) Override the Field method (see above), and implement your function there. 
//      DO NOT change the parameters or return type. 
//   3) Add a new kernel to VectorCompute.compute by adding the line
//          #pragma kernel [your class name]Field
//      at the top of the file and add the line
//          KERNEL_NAME([your class name])
//      at the bottom of the file. 
//   4) Add the name you want displayed for your field to the `enum FieldType` list
//      in VectorFields.cs. MAKE SURE that the order of this list is the same as the
//      order of the lines at the top of VectorCompute.compute. 
// %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

float3 Outwards(float3 position)
{
    return position;
};



float3 Swirl(float3 position)
{
    float3 val;
    val.x = -position.z;
    val.y = 0;
    val.z = position.x;
    return val;
};



float3 Coulomb(float3 position)
{
    float3 vect = float3(0.0, 0.0, 0.0);
    // The first argument in _FloatArgs is the number of charges in the system
    float numCharges = _FloatArgs[0];
    for (int i = 1; i < numCharges + 0; i++)
    {
        // The zeroth index of _VectorArgs is unused so that the two buffers align.
        float3 displacement = position - _VectorArgs[i];
        float distance = sqrt(displacement.x * displacement.x +
                displacement.y * displacement.y +
                displacement.z * displacement.z);
        vect += _FloatArgs[i] / (pow(distance, 3) + 0.0000000001) * displacement;
    }
    //Debugging:
    //vect.x += numCharges;
    return vect;
};

// Every type that's added must also be present in the enum in VectorFields.cs and have a kernel in VectorCompute.compute

