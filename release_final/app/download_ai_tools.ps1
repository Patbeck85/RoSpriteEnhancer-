# download_ai_tools.ps1 — Downloads alle KI-Engines für RoSpriteEnhancer
# Waifu2x, RealCUGAN, RealESRGAN, GFPGAN
$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "  DOWNLOADING ALL AI ENGINES FOR RO SPRITE ENHANCER       " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green

$toolsDir = Join-Path $PSScriptRoot "tools"
$waifuDir = Join-Path $toolsDir "waifu2x"
$cuganDir = Join-Path $toolsDir "realcugan"
$esrDir   = Join-Path $toolsDir "realesrgan"
$gfpDir   = Join-Path $toolsDir "gfpgan"

New-Item -ItemType Directory -Force -Path $waifuDir | Out-Null
New-Item -ItemType Directory -Force -Path $cuganDir | Out-Null
New-Item -ItemType Directory -Force -Path $esrDir | Out-Null
New-Item -ItemType Directory -Force -Path $gfpDir | Out-Null

# Waifu2x
$waifuZip = Join-Path $toolsDir "waifu2x.zip"
$waifuUrl = "https://github.com/nihui/waifu2x-ncnn-vulkan/releases/download/20220728/waifu2x-ncnn-vulkan-20220728-windows.zip"
Write-Host "`n[0/5] Lade Waifu2x NCNN Vulkan..." -ForegroundColor Cyan
Invoke-WebRequest -Uri $waifuUrl -OutFile $waifuZip
Expand-Archive -Path $waifuZip -DestinationPath $waifuDir -Force
Remove-Item $waifuZip -Force
$waifuSub = Get-ChildItem -Path $waifuDir -Directory
if ($waifuSub.Count -eq 1) { Get-ChildItem -Path $waifuSub[0].FullName | Move-Item -Destination $waifuDir -Force; Remove-Item $waifuSub[0].FullName -Force }

# RealCUGAN
$cuganZip = Join-Path $toolsDir "realcugan.zip"
$cuganUrl = "https://github.com/nihui/realcugan-ncnn-vulkan/releases/download/20220728/realcugan-ncnn-vulkan-20220728-windows.zip"
Write-Host "`n[1/5] Lade RealCUGAN NCNN Vulkan..." -ForegroundColor Cyan
Invoke-WebRequest -Uri $cuganUrl -OutFile $cuganZip
Expand-Archive -Path $cuganZip -DestinationPath $cuganDir -Force
Remove-Item $cuganZip -Force
$cuganSub = Get-ChildItem -Path $cuganDir -Directory
if ($cuganSub.Count -eq 1) { Get-ChildItem -Path $cuganSub[0].FullName | Move-Item -Destination $cuganDir -Force; Remove-Item $cuganSub[0].FullName -Force }

# 3. RealESRGAN NCNN Vulkan — Anime-Upscaling (Detail-Erhaltung)
Write-Host "`n[2/5] Lade RealESRGAN NCNN Vulkan..." -ForegroundColor Cyan
$esrZip = Join-Path $toolsDir "realesrgan.zip"
$esrUrl = "https://github.com/nihui/realesrgan-ncnn-vulkan/releases/download/20220728/realesrgan-ncnn-vulkan-20220728-windows.zip"
Invoke-WebRequest -Uri $esrUrl -OutFile $esrZip
Expand-Archive -Path $esrZip -DestinationPath $esrDir -Force
Remove-Item $esrZip -Force
$esrSub = Get-ChildItem -Path $esrDir -Directory
if ($esrSub.Count -eq 1) { Get-ChildItem -Path $esrSub[0].FullName | Move-Item -Destination $esrDir -Force; Remove-Item $esrSub[0].FullName -Force }

Write-Host "  Lade RealESRGAN Anime-Modell (x4plus-anime)..." -ForegroundColor Cyan
$esrModelUrl = "https://github.com/xinntao/Real-ESRGAN/releases/download/v0.1.0/RealESRGAN_x4plus_anime_6B.pth"
$esrModelPath = Join-Path $esrDir "realesrgan-x4plus-anime.pth"
try {
    Invoke-WebRequest -Uri $esrModelUrl -OutFile $esrModelPath
    Write-Host "  ✅ Anime-Modell gespeichert: $esrModelPath" -ForegroundColor Green
} catch {
    Write-Host "  ⚠️ Anime-Modell konnte nicht heruntergeladen werden (optional)" -ForegroundColor Yellow
}
Write-Host "  ✅ RealESRGAN installiert in: $esrDir" -ForegroundColor Green

# 4. GFPGAN — Gesicht & Haar Restaurierung (Details)
Write-Host "`n[3/5] Lade GFPGAN (Gesicht & Haar Restaurierung)..." -ForegroundColor Cyan
$gfpModelUrl = "https://github.com/TencentARC/GFPGAN/releases/download/v1.3.0/GFPGANv1.3.pth"
$gfpModelPath = Join-Path $gfpDir "GFPGANv1.3.pth"
try {
    Invoke-WebRequest -Uri $gfpModelUrl -OutFile $gfpModelPath
    Write-Host "  ✅ GFPGAN Modell gespeichert: $gfpModelPath" -ForegroundColor Green
} catch {
    Write-Host "  ⚠️ GFPGAN Modell konnte nicht heruntergeladen werden (optional)" -ForegroundColor Yellow
}

$gfpExeZip = Join-Path $toolsDir "gfpgan-ncnn.zip"
$gfpExeUrl = "https://github.com/nihui/gfpgan-ncnn-vulkan/releases/download/20220728/gfpgan-ncnn-vulkan-20220728-windows.zip"
try {
    Invoke-WebRequest -Uri $gfpExeUrl -OutFile $gfpExeZip
    Expand-Archive -Path $gfpExeZip -DestinationPath $gfpDir -Force
    Remove-Item $gfpExeZip -Force
    $gfpSub = Get-ChildItem -Path $gfpDir -Directory
    if ($gfpSub.Count -eq 1) { Get-ChildItem -Path $gfpSub[0].FullName | Move-Item -Destination $gfpDir -Force; Remove-Item $gfpSub[0].FullName -Force }
    Write-Host "  ✅ GFPGAN Executable installiert in: $gfpDir" -ForegroundColor Green
} catch {
    Write-Host "  ⚠️ GFPGAN Executable nicht verfügbar (nur Modell-Datei)" -ForegroundColor Yellow
}


Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "  ALLE 4 KI-ENGINES ERFOLGREICH INSTALLIERT!             " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Installationsübersicht:" -ForegroundColor White
Write-Host "  📁 waifu2x/      -> $waifuDir" -ForegroundColor White
Write-Host "  📁 realcugan/    -> $cuganDir" -ForegroundColor White
Write-Host "  📁 realesrgan/   -> $esrDir (inkl. x4plus-anime Modell)" -ForegroundColor White
Write-Host "  📁 gfpgan/       -> $gfpDir (inkl. GFPGANv1.3.pth)" -ForegroundColor White
Write-Host ""
Write-Host "Verwendung in RoSpriteEnhancer:" -ForegroundColor White
Write-Host "  Einstellungen -> Skalierung & KI -> Tool-Pfade eintragen" -ForegroundColor White
Write-Host "  Oder: Auto-Detect sucht automatisch im tools/-Ordner" -ForegroundColor White
