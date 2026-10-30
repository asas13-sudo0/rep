@echo off
setlocal
dotnet build "%~dp0RepoPhotonCloudMod.csproj" -c Release %*
if errorlevel 1 (
  echo Build failed. Check ManagedDir and BepInExCoreDir paths.
  exit /b 1
)
echo Copy bin\Release\net472\RepoPhotonCloudMod.dll to BepInEx\plugins\
