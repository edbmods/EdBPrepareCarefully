# EdB Prepare Carefully

## Contributing Translations

The more translations, the better--they are a great contribution to the mod community.  However, while it is appreciated that people take the time to translate the mod to other languages, it's difficult to keep up with those contributions.  Every addition or change to a translation requires a new release of the mod, and taking on that additional effort is not possible.

Therefore, translation pull requests will not be merged into the project.  Instead, please consider creating a standalone translation mod for your language.  These mods don't include any code--they simply include the translation resources for a given language.  With this approach, you can make changes and additions at your own pace, and you can take on the responsibility of releases for the translation.

## Building Prepare Carefully

The solution file was created using Xamarin Studio/MonoDevelop, but it should also work in Visual Studio.  Note that the project targets the older 3.5 version of the .NET framework used by the Unity engine on top of which RimWorld is built.

Note that the solution has dependencies on the following RimWorld DLLs:
- Assembly-CSharp.dll
- UnityEngine.CoreModule.dll
- UnityEngine.IMGUIModule
- UnityEngine.InputLegacyModule
- UnityEngine.TextRenderingModule

Copy those dependencies from the RimWorld game directory into the "Libraries" directory.  Be sure to make _copies_ of the originals--don't accidentally move/delete them from the original game directory.

The solution also has a dependency on the following third-party DLL:
- 0Harmony.dll (version 2.3.6)

The Harmony DLL is available from https://github.com/pardeike/Harmony/releases/tag/v2.3.6.0 and should be placed in the "Libraries/Harmony/2.3.6" directory.  Be sure to use the "net472" version from the Harmony-Thin download.

The result of the build will be the following DLL:
- EdBPrepareCarefully.dll

This DLL must be packaged alongside the contents of the `Resources` directory to create a working mod. The DLL built by the project should be placed inside a `1.6/Assemblies` directory.
If you need your mod package to support earlier versions of RimWorld, you'll need to download the latest Prepare Carefully DLL for each of the previous versions and place it in the correct `Assemblies` directory.
If you need your mod package to support version 1.4 or earlier of RimWorld, you'll need to download version 2.2.2.0 of Harmony and place it inside the `Pre1.5/Assemblies` directory.
The directory structure should look like this:

```
+ EdBPrepareCarefully
  + 1.2
    + Assemblies
      - EdBPrepareCarefully.dll
  + 1.3
    + Assemblies
      - EdBPrepareCarefully.dll
  + 1.4
    + Assemblies
      - EdBPrepareCarefully.dll
  + 1.5
    + Assemblies
      - EdBPrepareCarefully.dll
  + 1.6
    + Assemblies
      - EdBPrepareCarefully.dll
  + About
  + Common
  + Pre1.5
    + Assemblies
      - 0Harmony.dll
  - CHANGLELOG.txt
  - LICENSE
  - LoadFolders.xml
```

If Windows is the OS on which you're developing, you don't need to manually create the mod directory.  Instead, you can automatically package up the mod by running the `dist.bat` script.  This will copy all of the mod resources and the DLL into a `dist/EdBPrepareCarefully` directory.  Copy this `EdBPrepareCarefully` directory into your RimWorld `Mods` folder to use the mod in your game.
