@echo off
setlocal EnableDelayedExpansion

:: Liczniki
set "gfLines=0"
set "qnLines=0"
set "otherLines=0"

:: Zapytanie użytkownika
set /p userChoice="Chcesz policzyc rowniez pozostale pliki i cala sume? (T/N) "

echo Szukanie plikow .cs i zliczanie linii...

:: GameFiles
for /r ".\GameFiles" %%f in (*.cs) do (
    for /f %%l in ('type "%%f" ^| find /v /c ""') do (
        set /a gfLines+=%%l
    )
)

:: QuickNet
for /r ".\Plugins\QuickNet" %%f in (*.cs) do (
    for /f %%l in ('type "%%f" ^| find /v /c ""') do (
        set /a qnLines+=%%l
    )
)

:: Pozostale pliki (tylko jeśli wybrano)
if /i "!userChoice!"=="T" (
    for /r ".\" %%f in (*.cs) do (
        set "filePath=%%f"
        echo !filePath! | findstr /i /c:"\GameFiles\" >nul
        if errorlevel 1 (
            echo !filePath! | findstr /i /c:"\Libraries\QuickNet\" >nul
            if errorlevel 1 (
                for /f %%l in ('type "%%f" ^| find /v /c ""') do (
                    set /a otherLines+=%%l
                )
            )
        )
    )
)

:: Sumy
set /a sumTwo=gfLines+qnLines
set /a total=gfLines+qnLines+otherLines

:: Tabela
echo.
echo ====================== Podsumowanie ======================
echo Kategoria                     ^| Liczba linii
echo ---------------------------------------------------------
echo GameFiles                     ^| !gfLines!
echo QuickNet                      ^| !qnLines!
echo Suma GameFiles+QuickNet       ^| !sumTwo!
if /i "!userChoice!"=="T" (
    echo Pozostale pliki            ^| !otherLines!
    echo Caly projekt               ^| !total!
)
echo =========================================================
echo.
PAUSE
