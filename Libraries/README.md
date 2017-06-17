This directory should contain the DLL dependencies required to build the mod.
                                                                            
The solution has dependencies on the following two RimWorld DLLs:
- Assembly-CSharp.dll
- UnityEngine.dll

Copy those dependencies from the RimWorld game directory into the "Libraries" directory.  Be sure to make _copies_ of the originals--don't accidentally move/delete them from the original game directory.

The solution also has a dependency on the following third-party DLL:
- 0Harmony.dll

The Harmony DLL is available from https://github.com/pardeike/Harmony/releases and should also be placed in the "Libraries" directory.  Be sure to download and use the "Release" version.
