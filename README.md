# EdB Prepare Carefully

## Contributing Translations

The more translations, the better--they are a great contribution to the mod community.  However, while it is appreciated that people take the time to translate the mod to other languages, it's difficult to keep up with those contributions.  Every addition or change to a translation requires a new release of the mod, and taking on that additional effort is not possible.

Therefore, translation pull requests will not be merged into the project.  Please consider creating a standalone translation mod for your language like [this one](https://steamcommunity.com/sharedfiles/filedetails/?id=1205095547).  These mods don't include any code--they simply include the translation resources for a given language.  With this approach, you can make changes and additions at your own pace, and you can take on the responsibility of releases for the translation.

## Building Prepare Carefully

The solution file was created using Xamarin Studio/MonoDevelop, but it should also work in Visual Studio.  Note that the project targets the older 3.5 version of the .NET framework used by the Unity engine on top of which RimWorld is built.

Note that the solution has dependencies on the following RimWorld DLLs:
- Assembly-CSharp.dll
- UnityEngine.dll

Copy those dependencies from the RimWorld game directory into the "Libraries" directory.  Be sure to make _copies_ of the originals--don't accidentally move/delete them from the original game directory.

The solution also has a dependency on the following third-party DLL:
- 0Harmony.dll

The Harmony DLL is available from https://github.com/pardeike/Harmony/releases and should also be placed in the "Libraries" directory.  Be sure to download and use the "Release" version.

The result of the build will be the following DLL:
- EdBPrepareCarefully.dll

This DLL must be packaged with the contents of the `Resources` directory to create a working mod. The DLL should be placed inside an `Assemblies` directory.

To automatically build the mod directory for your Release DLL, run the `dist.bat` script.  This will copy all of the mod resources and the DLL into a `dist/EdBPrepareCarefully` directory.  Copy this `EdBPrepareCarefully` directory into your RimWorld `Mods` folder.


## Versioning

Prepare Carefully uses a versioning scheme inspired by [semantic versioning](http://semver.org/) that combines the major/minor version of RimWorld with the major/minor version of the mod to end up with the following format:

`{RimWorld major version}.{RimWorld minor version}.{mod version for this RimWorld version}`

Other conventions used to determine the mod version numbers:
- Mod version numbers always start at `1` and never start at `0`

Some examples:
+ **0.18.2**: The second release of the mod for the Beta 18 version of RimWorld
+ **1.0.1**: The first release of the mod for RimWorld 1.0
