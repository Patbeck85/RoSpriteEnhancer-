@echo off
title RO Sprite Enhancer — AI Tools Downloader
cd /d "%~dp0"
echo Lade Waifu2x, RealCUGAN und RIFE AI herunter...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0download_ai_tools.ps1"
pause
