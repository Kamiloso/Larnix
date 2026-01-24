@echo off
setlocal

set "_startdir=%~dp0Builds/"

for /d /r "%_startdir%" %%i in (Larnix_BurstDebugInformation_DoNotShip) do (
	if exist "%%i\" (
		echo Removing: "%%i"
		rd /s /q "%%i"
	)
)