# Vector Fields Implementation

## Installation

This repository may be cloned and the assets imported into your Unity project. The files in the main directory are interdependent and must all be present for any of them to function correctly. One part of this repository, containing some example files, must be unzipped. 

You may have to go into your project settings and check the box (currently in "Player settings") to "Allow unsafe code." I'm not sure why using `sizeof(Vector3)` requires this, but it does. 

I'll also try to make a custom Unity package of this, but there's no guarantees about that. 

See the **Example Files & Quickstart** section to get started. 



## Usage

For each field the user wishes to create and display, there are three essential components that must be attached to the GameObject:

* **FieldZone** determines the positions at which to perform the calculations. 
* **Display** receives calculated position and vector data and displays them, or processes them for the final result. 
* **VectorField** connects each part of the system together, calling each in turn and supplying data and references to each. It performs the calculations on the positions and passes that data to the display file. 

Once an has these three components (and their required components are attached—see below), it will be able to display a vector field when in play mode. By customizing components, the user can produce more specialized appearances and behaviors. 

### VectorField.cs

This is easily the most important part of the system and one of the few parts the user is not encouraged to edit or replace (except when modifying the field library—see below). To incorporate this component, a GameObject must have instances of `Display` and `FieldZone` already attached. These will be automatically detected, so the only reference the user needs to make is to attach `VectorCompute.compute` to the Compute Shader Field. 

Essentially, the VectorField's job is to make three calls per update. The first is to the FieldZone's `SetPositions` method, which initializes a buffer full of positions. The next is internal, to its compute shader, which calculates the value of the field at each point in the position buffer and stores that data in a vectors buffer. Finally, it calls the Display component's `DisplayVectors` method, giving it the positions and vectors buffers and inviting it to plot those however it likes. 

The easiest way for the user to change a part of this process is through the implementation of `SetPositions` or `DisplayVectors`. However, if you don't want to alter those, the VectorField offers three public delegates that run between these calls. 

* `preSetPositions` is called immediately before `SetPositions`
* `preCalculations` is called before the values of the field are evaluated
* `preDisplay` is called before `DisplayVectors`

You can subscribe functions to these, and access the public buffers in which the relevant data is stored, in order to tweak data, run debugging, or whatever else you feel is important during this time frame. For information on how to use delegates, the C# documentation on it is actually decent. 

Note that if the `isDynamic` is set to `false`, `DisplayVectors` and `preDisplay` will *not* be called after their initial call, which can be useful for preserving GPU resources in unchanging vector fields. 

### VectorCompute.compute

This is the other file that the user is not necessarily encouraged to implement themselves. It is short and simple: it receives position data in the form of a ComputeBuffer from the VectorField and runs parallel GPU threads to compute the vector field at each of those points in an efficient manner. The function that it uses to assign a value to a point is one of the ones in `FieldLibrary.hlsl`, chosen by the user. These functions take as input a position minus the `fieldOrigin`, the point set by the FieldZone to be treated as (0, 0, 0). Given a position at `positionsBuffer[i]`, the `float3` value of `vectorsBuffer[i]` (passed to the Display after calculation) is the calculated value of the vector field at that position.

However, you may wish to use a field with other arguments, such as an electric field that requires the position of point charges. Because there doesn't seem to be a way to implement arbitrary arguments, the compute shader file itself receives two additional buffers from the VectorField: `_FloatArgs` and `_VectorArgs`. You can assign these however you want and design your function in such a way that it reads them for the information it needs. For instance, to pass information about charge positions, you could use the first entry of `_FloatArgs` to pass the number of charges *n*, then the next *n* entries as the strength of each charge, while the corresponding entries in `_VectorArgs` store the positions of each charge. It's recommended, however, that whatever method you use to set these values is subscribed to the `preCalculations` delegate. 

### FieldLibrary.hlsl

This file is just a list of functions that take a `float3 position` (the HLSL version of a `Vector3`) as an argument and return a `float3` corresponding to the vector at that point. You are encouraged to add functions as you see fit, to create a variety of functions to choose from. 

To add the function `foo` to the list, you must do three things:

1. Add a function with the signature `float3 foo(float3 position)` to the file, implemented however you like. 

2. Add a new kernel to `VectorCompute.compute` by adding the line

   ```hlsl
   #pragma kernel fooField
   ```

   at the top of the file (*below* other such declarations) and adding 

   ```hlsl
   KERNEL_NAME(foo)
   ```

   to the bottom of the file (here order isn't important).

3. Finally, find the `public enum FieldType { ... }` declaration in `VectorFields.cs` and add `foo` to the end of the list inside the braces. MAKE SURE that the order of this list is the same as the
         order of the lines at the top of `VectorCompute.compute`.

Then, to display your function, select it from the "Field Type" drop-down menu in the inspector component of your VectorField.

### FieldZone.cs

This is an abstract class that contains a few essential values. 

* `positionsBuffer` is a ComputeBuffer of Vector3 values that correspond to the positions at which the values of the vector field will be calculated. 
* `maxVectorLength` is as it says: the maximum visual length that a displayed vector can have. This is used only by the Display component. 
* `fieldOrigin` is the point that you wish to use as (0, 0, 0) for vector field calculations. In situations where `_VectorArgs` are used to pass position data to the FieldLibrary function, this may get in the way of computation. If that is so, you can set it to zero from the FieldZone or add it *back* into the position in your FieldLibrary function (since it is subtracted from the worldspace position before being passed to the function).
* `bounds` is the Bounds object that will be used by the Display only. Depending on the implementation of the Display, it will likely make a `DrawMeshInstancedProcedural` call, which requires bounds so that the computer can determine when the camera isn't facing the drawn object. 
* `triggerCollider` is a collider that is used to determine when a FieldDetector is within the drawn field. 
* `canMove` is a boolean value that indicates whether the values of `positionsBuffer` might change each frame. It should be used in the implementation of `SetPositions` to reduce the strain on the GPU or CPU when last frame's position data is still suitable. It should be set by the user, the chosen implementation of FieldZone, or an external script; whatever is most appropriate. 

The class also contains two methods: 

* `SetPositions` is the class' defining method: it will be called each frame by the VectorField and it is expected that, after each call to it, the `positionsBuffer` contains the position data needed for that frame's calculations. 
* `Initialize` should be called at the beginning of `SetPositions` and be defined in such a way that it is only implemented once before `positionsBuffer` is destroyed. This should be the function that allocates the memory space for `positionsBuffer`, as well as setting whatever other variables will not change throughout the FieldZone's run time. 

In implementing this abstract class, you may make some choices about which values to calculate and which to leave to the user, or just not use. However, at the very least, `positionsBuffer` and `SetPositions` should be well-defined, as the VectorField will not function without those. 

### Display.cs

Display is also an abstract class, defined with just three fields. `bounds` and `maxVectorLength` are both set by VectorField based on their values in the FieldZone, but `pointerMaterial`, set by the user defines the material that will be used to display the field itself. 

The core of the Display class is the `DisplayVectors` method, which takes in the `positionsBuffer` set by the FieldZone and the `vectorsBuffer` calculated by `VectorCompute.compute`. It may do whatever you choose with this data but, seeing as how the data is already stored in ComputeBuffers, it's recommended to send this information to a shader that can draw the objects through procedural instancing. I'm not an expert on that, but any half-decent Internet tutorial on compute shaders (such as the one by Catlike Coding) will give you the basics. 

### FieldDetector.cs

You may have noticed that I haven't talked at all about this class at all yet. That's because it doesn't really fit into the framework of this package coherently, since it's not used to calculate or display vector fields. However, it's directly referenced by `FieldZone.cs`, so it needs to be included. You can feel free to remove this class and the two references in the FieldZone class file. 

If you do wish to implement it, this is another class designed to be inherited from (although it's not abstract). The idea is that it characterizes objects whose behaviors change within the boundaries of a field and who may wish to measure properties of that field. It's got built-in methods for entering and leaving a field, which update a reference to the field it's currently in. 



## Example Files & Quickstart

Included in this repository are two folders of examples. One includes meshes that one could use as vectors in the field display, and the other includes example code and assets. This second folder, Example Implementations, currently contains a prefab Vector Graph object which may or may not work out-of-the-box (my bet is on no) and a zipped file containing an example implementation of FieldZone (called RectZone) and an example implementation of Display (called VectorDisplay). To quickstart a field object, add a box collider (set to trigger), a RectZone component, a VectorDisplay component, and a VectorField component to it. Then add in references for the components as specified below. 

### RectZone

This component requires:

* A reference to the object's BoxCollider
* A reference to the compute shader called `RectPoints.compute`
* Specifications for the number of vectors in each direction
* The spacing between vectors
* The scaling factor, which is multiplied by the spacing to give the maximum visual magnitude of a vector

This class defines a grid arrangement of points, evenly spaced and for set distances in the x, y, and z directions. If `canMove` is set to true, the grid will rotate or shift with the motion of the parent transform. 

### VectorDisplay

This component requires:

* A reference to a material that implements the `VectorShader.shader` shader. Each field in the scene must have its own material, and each material must have "GPU instancing" enabled. 
* A mesh to use as the vector "arrow." A few are provided, but feel free to get creative. 
* A reference to the `VectorDisplay.compute` compute shader

This class defines the "typical" vector field: arrows rooted at their points, with lengths and colors that correspond to the magnitude of the field. 



## Requirements

These programs are meant to be run in Unity (last tested in version 2020.3.10f1), so they require the UnityEngine package. The core of this system centers around the use of compute shaders, so the user must be able to use those in whatever device the project is being constructed for.

There are probably some other important considerations here, especially to use the shaders included, but I'm really not familiar enough with shaders or graphics to be able to say for sure. 

README last updated 6/22/2021

**Note:** I'm currently a student, working full time on this (and a related project), so I'm able to maintain this at the moment. However, I cannot guarantee that in the future. I also can't guarantee that this code will satisfy you if you've got actual experience, seeing as how I've got four weeks of C# and HLSL under my belt at the time of writing. You're welcome to create issues and pull requests on the GitHub repo, or send me suggestions, but know that I won't necessarily have time to get to them and, even if I do, I might not approve or address them if they're beyond my understanding, even if you may feel dead certain that they're necessary. That's just because I'll have other things to do with my time than curate this package. However, if you do want to improve the repo, feel free to fork it, make changes, and share. Just give credit where credit is due. 