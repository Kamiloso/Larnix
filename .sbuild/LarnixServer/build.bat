@echo off
setlocal DisableDelayedExpansion
set "_DIR_=%~dp0build"

dotnet clean

if "%~1"=="" (
    set "windows=1"
    set "linux=1"
    set "mac-intel=1"
    set "mac-silicon=1"
    goto :build
)

:check
set "_a=%~1"
if defined _a (
	for %%n in (windows, linux, mac-intel, mac-silicon) do (
		if "%_a%"=="%%n" set "%%n=1"
	)
	shift
	goto :check
)

:build
if defined windows (
	if exist "%_DIR_%\Windows\" rmdir /s /q "%_DIR_%\Windows"
	dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -o "%_DIR_%\Windows"
	del /f /q "%_DIR_%\Windows\*.pdb"
)
if defined linux (
	if exist "%_DIR_%\Linux\" rmdir /s /q "%_DIR_%\Linux"
	dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -o "%_DIR_%\Linux"
	del /f /q "%_DIR_%\Linux\*.pdb"
)
if defined mac-intel (
	if exist "%_DIR_%\Mac_Intel\" rmdir /s /q "%_DIR_%\Mac_Intel"
	dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -o "%_DIR_%\Mac_Intel"
	del /f /q "%_DIR_%\Mac_Intel\*.pdb"
)
if defined mac-silicon (
	if exist "%_DIR_%\Mac_Silicon\" rmdir /s /q "%_DIR_%\Mac_Silicon"
	dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -o "%_DIR_%\Mac_Silicon"
	del /f /q "%_DIR_%\Mac_Silicon\*.pdb"
)

exit/b