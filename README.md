# EdB Prepare Carefully

## Building Prepare Carefully

Open the solution file in either Xamarin Studio/MonoDevelop or in Visual Studio.

Note that the solution has dependencies on the following DLLs:
- Assembly-CSharp.dll
- UnityEngine.dll

You'll need to add those dependencies to your project.  Since they might reference file paths on your local development environment, I did not include them in the project files.

The result of the build will be the following DLL:
- EdBPrepareCarefully.dll

This DLL should be packaged with the contents of the `Resources` directory, inside a `Resources/Assemblies` directory.

The build does not automate the creation of the mod distribution directory.

