# EdB Prepare Carefully

## Contributing Translations

The more translations, the better--they are a great contribution to the mod community.  However, while it is appreciated that people take the time to translate the mod to other languages, it's difficult to keep up with those contributions.  Every addition or change to a translation requires a new release of the mod, and taking on that additional effort is not possible.

Therefore, translation pull requests will not be merged into the project.  Please consider creating a standalone translation mod for your language like [this one](https://steamcommunity.com/sharedfiles/filedetails/?id=1205095547).  These mods don't include any code--they simply include the translation resources for a given language.  With this approach, you can make changes and additions at your own pace, and you can take on the responsibility of releases for the translation.

## Building Prepare Carefully

Note that the project targets the older 3.5 version of the .NET framework used by the Unity engine on top of which RimWorld is built.

Note that the solution has dependencies on the following RimWorld DLLs:
- Assembly-CSharp.dll
- UnityEngine.CoreModule.dll
- UnityEngine.IMGUIModule
- UnityEngine.InputLegacyModule
- UnityEngine.TextRenderingModule

Depending on your system you may need to update the assembly references.

The solution also has a dependency on the following third-party DLL provided through the nuget package manager:
- 0Harmony.dll

In the event that the nuget package isn't working or up to date, the Harmony DLL is also available from https://github.com/pardeike/Harmony/releases.  Prepare Carefully uses version 2.0.0.8 of Harmony. When you download Harmony, you'll see multiple versions of the DLL organized into various directories.  Be sure to use the one in the "Release/net472" directory.

Only if you _must_ create a build that also supports RimWorld 1.0, you will need to get the DLL from the latest Prepare Carefully release for 1.0, along with the DLL for Harmony _1.2_.  Place these DLLs into the `Resources/Assemblies` directory.

The result of the build will be the following DLL:
- EdBPrepareCarefully.dll

## Versioning

Prepare Carefully uses a versioning scheme inspired by [semantic versioning](http://semver.org/) that combines the major/minor version of RimWorld with the major/minor version of the mod to end up with the following format:

`{RimWorld major version}.{RimWorld minor version}.{mod version for this RimWorld version}`

Other conventions used to determine the mod version numbers:
- Mod version numbers always start at `1` and never start at `0`

Some examples:
+ **0.18.2**: The second release of the mod for the Beta 18 version of RimWorld
+ **1.0.11**: The eleventh release of the mod for RimWorld 1.0
+ **1.1.1**: The first release of the mod for RimWorld 1.1
