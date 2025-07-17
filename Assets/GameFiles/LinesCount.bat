@echo off
setlocal EnableDelayedExpansion

set "lineCount=0"

echo Szukanie plikow .cs i zliczanie linii...

for /r %%f in (*.cs) do (
    for /f %%l in ('type "%%f" ^| find /v /c ""') do (
        set /a lineCount+=%%l
    )
)

echo.
echo Znaleziono lacznie !lineCount! linii we wszystkich plikach .cs.
echo.
PAUSE
