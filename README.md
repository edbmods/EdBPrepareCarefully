# EdB Prepare Carefully

## Building Prepare Carefully

The solution file was created using Xamarin Studio/MonoDevelop, but it should also work in Visual Studio.  Note that the project targets the older 3.5 version of the .NET framework used by the Unity engine on top of which RimWorld is built.

Note that the solution has dependencies on the following DLLs:
- Assembly-CSharp.dll
- UnityEngine.dll

Copy those dependencies from the RimWorld game directory into the "Libraries" directory.  Be sure to make _copies_ of the originals--don't accidentally move/delete them from the original game directory.

The result of the build will be the following DLL:
- EdBPrepareCarefully.dll

This DLL should be packaged with the contents of the `Resources` directory, inside a `Resources/Assemblies` directory.

The build does not automate the creation of the mod distribution directory.

## Versioning

Prepare Carefully uses a versioning scheme inspired by [semantic versioning](http://semver.org/) that combines the major/minor version of RimWorld with the major/minor version of the mod to end up with the following format:

`{RimWorld major version}.{RimWorld minor version}.{mod major version}.{mod minor version}`

Other conventions used to determine the mod version numbers:
- The mod only uses `0` for major versions that are considered unstable/beta releases (i.e. 0.15.0.1)
- Minor version numbers always start at `1` and never start at `0`
- The mod considers a major release to be a release that adds or removes features
- The mod considers a minor release to be an incremental release that makes bug fixes or otherwise changes existing features.

Some examples:
+ **0.14.0.5**: The fifth "beta"/unstable release of the mod for RimWorld Alpha 14
+ **0.14.1.1**: The first stable release of the mod for RimWorld Alpha 14
+ **0.14.2.1**: The second stable release of the mod for RimWorld Alpha 14.  The point system was re-enabled for this version.  Since this was a new feature that was not in the previous version, it needed a new major-version number.
+ **0.14.2.2**: If we need one, this will be the version number for the first bug-fix release after the second stable release of the mod for RimwWorld Alpha 14
+ **0.15.0.1**: The first "beta"/unstable version of the mod for RimWorld Alpha 15
