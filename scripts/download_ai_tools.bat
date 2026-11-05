@echo off
title RO Sprite Enhancer — AI Tools Downloader
cd /d "%~dp0"
echo Lade alle KI-Engines herunter (Waifu2x, RealCUGAN, RealESRGAN, GFPGAN)...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0download_ai_tools.ps1"
pause
