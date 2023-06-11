@echo off
setlocal EnableDelayedExpansion

cd /D "%~dp0"
set "CENTURIA_LAUNCHER_PATH=%~0"
SET libs=
for %%i in (*.jar) do SET libs=!libs!;%%i
for /r "./libs" %%i in (*.jar) do SET libs=!libs!;%%i
SET libs=%libs:~1%

start win\java-17\bin\javaw -cp "%libs%" org.asf.centuria.launcher.updater.LauncherUpdaterMain %*
