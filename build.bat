@echo off
setlocal enabledelayedexpansion
title RO Sprite Enhancer â€” Compiler

:: WICHTIG: Wechselt immer in den Ordner in dem DIESE .bat-Datei liegt
cd /d "%~dp0"

echo.
echo  =====================================================
echo    RO Sprite Enhancer â€” Build ^& Compiler Script
echo    Teil des Yggdrasil.Studio Oekosystems
echo  =====================================================
echo.

:: â”€â”€â”€ VERSION â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
set VERSION=1.20.0
set EXE_NAME=RoSpriteEnhancer
set OUT_DIR=bin\Release\%EXE_NAME%_v%VERSION%
set ZIP_NAME=%EXE_NAME%_v%VERSION%.zip

:: â”€â”€â”€ .NET SDK PRUEFEN â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo [1/5] PrÃ¼fe .NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo  FEHLER: .NET SDK nicht gefunden!
    echo  Bitte installieren: https://dotnet.microsoft.com/download
    echo  Benoetigt: .NET 8 SDK oder hoeher
    echo.
    pause
    exit /b 1
)

for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do set DOTNET_VER=%%v
echo  âœ“ .NET SDK gefunden: v%DOTNET_VER%

:: â”€â”€â”€ PROJEKTDATEI PRUEFEN â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo [2/5] PrÃ¼fe Projektstruktur...
if not exist "%EXE_NAME%.csproj" (
    echo.
    echo  FEHLER: %EXE_NAME%.csproj nicht gefunden!
    echo  Bitte build.bat aus dem Projektordner starten.
    echo.
    pause
    exit /b 1
)
echo  âœ“ Projektdatei gefunden

:: â”€â”€â”€ ALTEN BUILD AUFRAUMEN â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo [3/5] Bereinige alten Build...
if exist "%OUT_DIR%" (
    rmdir /s /q "%OUT_DIR%"
    echo  âœ“ Alter Build-Ordner geleert
) else (
    echo  âœ“ Kein alter Build vorhanden
)

:: â”€â”€â”€ KOMPILIEREN â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo [4/5] Kompiliere Release...
echo.

:: Sauberer 1-Zeilen-Publish-Befehl ohne kaputte Carets/Backslashes
dotnet restore %EXE_NAME%.csproj && dotnet publish %EXE_NAME%.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:DebugSymbols=false -p:AssemblyVersion=%VERSION%.0 -p:FileVersion=%VERSION%.0 -o "%OUT_DIR%"

if %errorlevel% neq 0 (
    echo.
    echo  â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
    echo  â•‘   BUILD FEHLGESCHLAGEN!              â•‘
    echo  â•‘   Fehler siehe Ausgabe oben.         â•‘
    echo  â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    echo.
    pause
    exit /b 1
)

:: â”€â”€â”€ EXE GROESSE ANZEIGEN â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
if exist "%OUT_DIR%\%EXE_NAME%.exe" (
    for %%f in ("%OUT_DIR%\%EXE_NAME%.exe") do (
        set /a SIZE_KB=%%~zf / 1024
        set /a SIZE_MB=%%~zf / 1048576
    )
    echo  âœ“ EXE erstellt: %EXE_NAME%.exe (!SIZE_MB! MB)
)

:: â”€â”€â”€ README KOPIEREN â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
if exist "README.md" (
    copy /y "README.md" "%OUT_DIR%\README.md" >nul
if exist "download_ai_tools.ps1" copy /y "download_ai_tools.ps1" "%OUT_DIR%\download_ai_tools.ps1" >nul
if exist "download_ai_tools.bat" copy /y "download_ai_tools.bat" "%OUT_DIR%\download_ai_tools.bat" >nul
    echo  âœ“ README.md kopiert
)

:: â”€â”€â”€ ZIP PACKEN (optional via PowerShell) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo [5/5] Erstelle ZIP-Paket...
set ZIP_OUT=bin\Release\%ZIP_NAME%
if exist "%ZIP_OUT%" del /q "%ZIP_OUT%"

powershell -NoProfile -Command "Compress-Archive -Path '%OUT_DIR%\*' -DestinationPath '%ZIP_OUT%' -Force" >nul 2>&1

if %errorlevel% equ 0 (
    echo  âœ“ ZIP erstellt: %ZIP_NAME%
) else (
    echo  ! ZIP-Erstellung fehlgeschlagen
    echo    EXE liegt direkt in: %OUT_DIR%\
)

:: â”€â”€â”€ ERGEBNIS â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo.
echo  â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo  â•‘   BUILD ERFOLGREICH  âœ“                              â•‘
echo  â• â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•£
echo  â•‘   Version  : %VERSION%                                   â•‘
echo  â•‘   EXE      : %OUT_DIR%\%EXE_NAME%.exe                â•‘
echo  â•‘   ZIP      : bin\Release\%ZIP_NAME%                  â•‘
echo  â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

:: Optional: EXE direkt starten?
set /p START_NOW= EXE jetzt starten? (j/n): 
if /i "%START_NOW%"=="j" (
    start "" "%OUT_DIR%\%EXE_NAME%.exe"
)

endlocal
pause


