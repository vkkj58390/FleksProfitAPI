@echo off
REM === Læs settings ===
for /f "delims== tokens=1,2" %%G in (DockerSettings.txt) do set %%G=%%H

echo Eksporterer image: %NAME%:%VERSION%
cd ..

REM === Sørg for at mappen findes ===
if not exist DockerTools\DockerSaves mkdir DockerTools\DockerSaves

REM === Gem image til tar-fil ===
docker save %NAME%:%VERSION% -o "DockerTools\DockerSaves\%NAME%_%VERSION%.tar"

if %errorlevel% neq 0 (
    echo FEJL: Eksport mislykkedes.
    pause
    exit /b %errorlevel%
)

echo.
echo Image gemt som: DockerTools\DockerSaves\%NAME%_%VERSION%.tar
pause