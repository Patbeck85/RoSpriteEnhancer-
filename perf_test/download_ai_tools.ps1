# download_ai_tools.ps1 — Downloads Waifu2x, RealCUGAN, and RIFE 60 FPS AI
$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "  DOWNLOADING ALL AI ENGINES (WAIFU2X, REALCUGAN, RIFE)   " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green

$toolsDir = Join-Path $PSScriptRoot "tools"
$waifuDir = Join-Path $toolsDir "waifu2x"
$cuganDir = Join-Path $toolsDir "realcugan"
$rifeDir  = Join-Path $toolsDir "rife"

New-Item -ItemType Directory -Force -Path $waifuDir | Out-Null
New-Item -ItemType Directory -Force -Path $cuganDir | Out-Null
New-Item -ItemType Directory -Force -Path $rifeDir  | Out-Null

# Waifu2x
$waifuZip = Join-Path $toolsDir "waifu2x.zip"
$waifuUrl = "https://github.com/nihui/waifu2x-ncnn-vulkan/releases/download/20220728/waifu2x-ncnn-vulkan-20220728-windows.zip"
Write-Host "
[1/3] Lade Waifu2x NCNN Vulkan..." -ForegroundColor Cyan
Invoke-WebRequest -Uri $waifuUrl -OutFile $waifuZip
Expand-Archive -Path $waifuZip -DestinationPath $waifuDir -Force
Remove-Item $waifuZip -Force
$waifuSub = Get-ChildItem -Path $waifuDir -Directory
if ($waifuSub.Count -eq 1) { Get-ChildItem -Path $waifuSub[0].FullName | Move-Item -Destination $waifuDir -Force; Remove-Item $waifuSub[0].FullName -Force }

# RealCUGAN
$cuganZip = Join-Path $toolsDir "realcugan.zip"
$cuganUrl = "https://github.com/nihui/realcugan-ncnn-vulkan/releases/download/20220728/realcugan-ncnn-vulkan-20220728-windows.zip"
Write-Host "
[2/3] Lade RealCUGAN NCNN Vulkan..." -ForegroundColor Cyan
Invoke-WebRequest -Uri $cuganUrl -OutFile $cuganZip
Expand-Archive -Path $cuganZip -DestinationPath $cuganDir -Force
Remove-Item $cuganZip -Force
$cuganSub = Get-ChildItem -Path $cuganDir -Directory
if ($cuganSub.Count -eq 1) { Get-ChildItem -Path $cuganSub[0].FullName | Move-Item -Destination $cuganDir -Force; Remove-Item $cuganSub[0].FullName -Force }

# RIFE 60 FPS
$rifeZip = Join-Path $toolsDir "rife.zip"
$rifeUrl = "https://github.com/nihui/rife-ncnn-vulkan/releases/download/20221029/rife-ncnn-vulkan-20221029-windows.zip"
Write-Host "
[3/3] Lade RIFE 60 FPS AI Engine..." -ForegroundColor Cyan
Invoke-WebRequest -Uri $rifeUrl -OutFile $rifeZip
Expand-Archive -Path $rifeZip -DestinationPath $rifeDir -Force
Remove-Item $rifeZip -Force
$rifeSub = Get-ChildItem -Path $rifeDir -Directory
if ($rifeSub.Count -eq 1) { Get-ChildItem -Path $rifeSub[0].FullName | Move-Item -Destination $rifeDir -Force; Remove-Item $rifeSub[0].FullName -Force }

Write-Host "
==========================================================" -ForegroundColor Green
Write-Host "  ALLE 3 KI-ENGINES ERFOLGREICH INSTALLIERT!             " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
