# EdB Prepare Carefully

## Building Prepare Carefully

Open the solution file in either Xamarin Studio/MonoDevelop or in Visual Studio.

Note that the solution has dependencies on the following DLLs:
- Assembly-CSharp.dll
- UnityEngine.dll

Copy those dependencies from the RimWorld game directory into the "Libraries" directory.  Be sure to make _copies_ of the originals--don't accidentally move/delete them from the original game directory.

The result of the build will be the following DLL:
- EdBPrepareCarefully.dll

This DLL should be packaged with the contents of the `Resources` directory, inside a `Resources/Assemblies` directory.

The build does not automate the creation of the mod distribution directory.

