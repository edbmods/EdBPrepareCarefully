This directory should contain the DLL dependencies required to build the mod.
                                                                            
The solution has dependencies on the following RimWorld DLLs:
- Assembly-CSharp.dll
- UnityEngine.CoreModule.dll
- UnityEngine.IMGUIModule.dll
- UnityEngine.InputLegacyModule.dll
- UnityEngine.TextRenderingModule.dll

Copy those dependencies from the RimWorld game directory into the "Libraries" directory.  Be sure to make _copies_ of the originals--don't accidentally move/delete them from the original game directory.

The solution also has a dependency on the following third-party DLL:
- 0Harmony.dll (version 2.3.6)

The Harmony DLL is available from https://github.com/pardeike/Harmony/releases/tag/v2.3.6.0 and should be placed in the "Libraries/Harmony/2.3.6" directory.  Be sure to use the "net472" version from the Harmony-Thin download.
