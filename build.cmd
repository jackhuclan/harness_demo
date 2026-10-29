@echo off
REM 一键构建：调用 build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
pause
