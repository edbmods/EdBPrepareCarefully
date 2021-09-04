@echo off
if exist bin\Release\EdBPrepareCarefully.dll (
	robocopy Resources dist\EdBPrepareCarefully\ /e /MIR
	xcopy LICENSE dist\EdBPrepareCarefully\ /Y
	xcopy bin\Release\EdBPrepareCarefully.dll dist\EdBPrepareCarefully\1.3\Assemblies\ /Y
	xcopy Libraries\Harmony\2.0\0Harmony.dll dist\EdBPrepareCarefully\Common\Assemblies\ /Y
	xcopy Libraries\EdBPrepareCarefully\1.2\EdBPrepareCarefully.dll dist\EdBPrepareCarefully\1.2\Assemblies\ /Y
	xcopy THIRD-PARTY-LICENSES dist\EdBPrepareCarefully\Common\Assemblies\ /Y
	del dist\EdBPrepareCarefully\Assemblies\README.md
	del dist\EdBPrepareCarefully\Latest\Assemblies\README.md
	exit /b 0
) else (
	echo Cannot build mod distribution directory. Release build not found in bin\Release directory.
	pause
)
