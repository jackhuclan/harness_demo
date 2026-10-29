# 一键启动：新开窗口跑后端(5000) -> 等3秒 -> 新开窗口跑前端(3000)
$root = $PSScriptRoot
$dotnet = if (Test-Path "C:\Program Files\dotnet\dotnet.exe") { "C:\Program Files\dotnet\dotnet.exe" } else { "dotnet" }

Write-Host "[1/2] 启动后端 ASP.NET Core (http://localhost:5000/swagger)" -ForegroundColor Cyan
Start-Process -FilePath "cmd.exe" -ArgumentList "/k", "cd /d `"$root`" && `"$dotnet`" run --project src\HarnessDemo.Host --urls http://localhost:5000" -WindowStyle Normal

Start-Sleep -Seconds 3

Write-Host "[2/2] 启动前端 Next.js (http://localhost:3000)" -ForegroundColor Cyan
Start-Process -FilePath "cmd.exe" -ArgumentList "/k", "cd /d `"$root\chat-demo`" && npm run dev" -WindowStyle Normal

Write-Host "`n已启动两个窗口：" -ForegroundColor Green
Write-Host "  后端: http://localhost:5000/swagger"
Write-Host "  前端: http://localhost:3000"
