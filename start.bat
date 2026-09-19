@echo off
chcp 65001 >nul
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

set "DOCKER_DESKTOP=%ProgramFiles%\Docker\Docker\Docker Desktop.exe"
set "DOCKER_BIN=%ProgramFiles%\Docker\Docker\resources\bin"

where docker >nul 2>&1
if errorlevel 1 (
  call :install_docker
  if errorlevel 1 exit /b 1
)

docker info >nul 2>&1
if errorlevel 1 (
  echo [ForestProof] Запускаю Docker Desktop...
  if exist "%DOCKER_DESKTOP%" start "" "%DOCKER_DESKTOP%"
)

call :wait_docker
if errorlevel 1 (
  echo [ForestProof] Docker не запустился. Перезагрузите Windows или запустите Docker Desktop вручную.
  exit /b 1
)

docker compose version >nul 2>&1
if errorlevel 1 (
  echo [ForestProof] Не найден плагин "docker compose". Обновите Docker Desktop.
  exit /b 1
)

if not "%~1"=="" (
  echo [ForestProof] docker compose %*
  docker compose %*
  exit /b %errorlevel%
)

echo [ForestProof] Собираю и запускаю ForestProof...
docker compose up --build -d
if errorlevel 1 exit /b 1
docker compose ps

echo.
echo ForestProof запущен:
echo   Frontend:  http://localhost:5173
echo   Gateway:   http://localhost:5300/healthz
echo   PostGIS:   localhost:5432 (forestproof/forestproof)
echo.
echo Полезные команды:
echo   start.bat logs -f   - логи
echo   start.bat ps        - статус
echo   start.bat down      - остановить
echo   start.bat down -v   - остановить и удалить БД
exit /b 0

:install_docker
where winget >nul 2>&1
if not errorlevel 1 (
  echo [ForestProof] Устанавливаю Docker Desktop через winget...
  winget install --id Docker.DockerDesktop -e --accept-source-agreements --accept-package-agreements
  goto install_done
)
where choco >nul 2>&1
if not errorlevel 1 (
  echo [ForestProof] Устанавливаю Docker Desktop через Chocolatey...
  choco install docker-desktop -y
  goto install_done
)
echo [ForestProof] Не найден ни winget, ни choco.
echo Установите Docker Desktop вручную: https://www.docker.com/products/docker-desktop/
exit /b 1

:install_done
echo [ForestProof] Docker Desktop установлен. Может потребоваться перезагрузка или повторный вход.
set "PATH=%DOCKER_BIN%;%PATH%"
exit /b 0

:wait_docker
set /a tries=0
:wait_loop
docker info >nul 2>&1
if not errorlevel 1 exit /b 0
set /a tries+=1
if !tries! geq 60 exit /b 1
timeout /t 3 /nobreak >nul
goto wait_loop
