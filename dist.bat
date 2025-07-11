@echo off
if exist bin\Release\EdBPrepareCarefully.dll (
	robocopy Resources dist\EdBPrepareCarefully\ /e /MIR
	xcopy LICENSE dist\EdBPrepareCarefully\ /Y
	xcopy bin\Release\EdBPrepareCarefully.dll dist\EdBPrepareCarefully\1.6\Assemblies\ /Y
	xcopy Libraries\EdBPrepareCarefully\1.2\EdBPrepareCarefully.dll dist\EdBPrepareCarefully\1.2\Assemblies\ /Y
	xcopy Libraries\EdBPrepareCarefully\1.3\EdBPrepareCarefully.dll dist\EdBPrepareCarefully\1.3\Assemblies\ /Y
	xcopy Libraries\EdBPrepareCarefully\1.4\EdBPrepareCarefully.dll dist\EdBPrepareCarefully\1.4\Assemblies\ /Y
	xcopy Libraries\EdBPrepareCarefully\1.5\EdBPrepareCarefully.dll dist\EdBPrepareCarefully\1.5\Assemblies\ /Y
	xcopy Libraries\Harmony\2.2.2\0Harmony.dll dist\EdBPrepareCarefully\Pre1.5\Assemblies\ /Y
	xcopy THIRD-PARTY-LICENSES dist\EdBPrepareCarefully\Pre1.5\Assemblies\ /Y
	del dist\EdBPrepareCarefully\Assemblies\README.md
	del dist\EdBPrepareCarefully\Latest\Assemblies\README.md
	exit /b 0
) else (
	echo Cannot build mod distribution directory. Release build not found in bin\Release directory.
	pause
)
