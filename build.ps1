# 一键构建：后端 .NET（build + test）+ 前端 Next.js
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$dotnet = if (Test-Path "C:\Program Files\dotnet\dotnet.exe") { "C:\Program Files\dotnet\dotnet.exe" } else { "dotnet" }

Write-Host "===== [1/3] 构建后端 .NET =====" -ForegroundColor Cyan
& $dotnet build (Join-Path $root "HarnessDemo.slnx") -c Release --nologo
if ($LASTEXITCODE -ne 0) { Write-Host "后端构建失败" -ForegroundColor Red; exit 1 }

Write-Host "`n===== [2/3] 运行后端测试 =====" -ForegroundColor Cyan
& $dotnet test (Join-Path $root "tests/HarnessDemo.Tests/HarnessDemo.Tests.csproj") -c Release --no-build --nologo
if ($LASTEXITCODE -ne 0) { Write-Host "测试未通过" -ForegroundColor Red; exit 1 }

Write-Host "`n===== [3/3] 构建前端 Next.js =====" -ForegroundColor Cyan
$fe = Join-Path $root "chat-demo"
if (-not (Test-Path (Join-Path $fe "node_modules"))) {
    Write-Host "首次构建，安装前端依赖..."
    Push-Location $fe; npm install; Pop-Location
}
Push-Location $fe; npm run build; $code = $LASTEXITCODE; Pop-Location
if ($code -ne 0) { Write-Host "前端构建失败" -ForegroundColor Red; exit 1 }

Write-Host "`n===== 构建+测试完成 =====" -ForegroundColor Green
