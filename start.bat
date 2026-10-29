@echo off
REM 一键启动：先后端，再前端（各开一个窗口）
setlocal
set ROOT=%~dp0

echo [1/2] 启动后端 ASP.NET Core (http://localhost:5000/swagger)
start "Harness Backend" cmd /k "cd /d %ROOT% && ""C:\Program Files\dotnet\dotnet.exe"" run --project src\HarnessDemo.Host --urls http://localhost:5000"

REM 等后端起来
timeout /t 3 /nobreak >nul

echo [2/2] 启动前端 Next.js (http://localhost:3000)
start "Chat Frontend" cmd /k "cd /d %ROOT%chat-demo && npm run dev"

echo.
echo 已启动两个窗口：
echo   后端: http://localhost:5000/swagger
echo   前端: http://localhost:3000
endlocal
