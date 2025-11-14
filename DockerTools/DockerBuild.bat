@echo off
REM === Læs settings fra fil ===
for /f "delims== tokens=1,2" %%G in (DockerSettings.txt) do set %%G=%%H

echo Bygger Docker image: %NAME%:%VERSION%
cd ..

REM === Byg image (samme context som docker-compose.yml) ===
docker build -t %NAME%:%VERSION% -f FleksProfitAPI/Dockerfile ./FleksProfitAPI

if %errorlevel% neq 0 (
    echo FEJL: Build mislykkedes.
    pause
    exit /b %errorlevel%
)

echo.
echo Docker image bygget: %NAME%:%VERSION%
pause