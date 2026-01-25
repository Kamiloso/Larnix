@echo off
setlocal DisableDelayedExpansion
set "_DIR_=%~dp0build"
set "_BUILD_=%~dp0..\..\Builds"

if "%~1"=="" (
    set "windows=1"
    set "linux=1"
    set "mac-intel=1"
    set "mac-silicon=1"
    goto :move
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

:move
if defined windows (
	if exist "%_BUILD_%\Windows\Server" rmdir /s /q "%_BUILD_%\Windows\Server"
    robocopy "%_DIR_%\Windows" "%_BUILD_%\Windows\Server" /E /MOVE /IS >nul 2>&1
    if errorlevel 8 goto :error
)
if defined linux (
	if exist "%_BUILD_%\Linux\Server" rmdir /s /q "%_BUILD_%\Linux\Server"
    robocopy "%_DIR_%\Linux" "%_BUILD_%\Linux\Server" /E /MOVE /IS >nul 2>&1
    if errorlevel 8 goto :error
)
if defined mac-intel (
	if exist "%_BUILD_%\Mac_Intel\Server" rmdir /s /q "%_BUILD_%\Mac_Intel\Server"
    robocopy "%_DIR_%\Mac_Intel" "%_BUILD_%\Mac_Intel\Server" /E /MOVE /IS >nul 2>&1
    if errorlevel 8 goto :error
)
if defined mac-silicon (
	if exist "%_BUILD_%\Mac_Silicon\Server" rmdir /s /q "%_BUILD_%\Mac_Silicon\Server"
    robocopy "%_DIR_%\Mac_Silicon" "%_BUILD_%\Mac_Silicon\Server" /E /MOVE /IS >nul 2>&1
    if errorlevel 8 goto :error
)

exit/b 0

:error
exit/b 1