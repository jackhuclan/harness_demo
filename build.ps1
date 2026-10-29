# 一键构建：后端 .NET + 前端 Next.js
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "===== [1/2] 构建后端 .NET =====" -ForegroundColor Cyan
$dotnet = if (Test-Path "C:\Program Files\dotnet\dotnet.exe") { "C:\Program Files\dotnet\dotnet.exe" } else { "dotnet" }
& $dotnet build (Join-Path $root "HarnessDemo.slnx") -c Release --nologo
if ($LASTEXITCODE -ne 0) { Write-Host "后端构建失败" -ForegroundColor Red; exit 1 }

Write-Host "`n===== [2/2] 构建前端 Next.js =====" -ForegroundColor Cyan
$fe = Join-Path $root "chat-demo"
if (-not (Test-Path (Join-Path $fe "node_modules"))) {
    Write-Host "首次构建，安装前端依赖..."
    Push-Location $fe; npm install; Pop-Location
}
Push-Location $fe; npm run build; $code = $LASTEXITCODE; Pop-Location
if ($code -ne 0) { Write-Host "前端构建失败" -ForegroundColor Red; exit 1 }

Write-Host "`n===== 构建完成 =====" -ForegroundColor Green
