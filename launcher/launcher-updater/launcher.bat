@echo off
setlocal EnableDelayedExpansion

:MAIN
SET libs=
for %%i in (*.jar) do SET libs=!libs!;%%i
for /r "./libs" %%i in (*.jar) do SET libs=!libs!;%%i
SET libs=%libs:~1%

start java-17\bin\javaw -cp "%libs%" org.asf.centuria.launcher.updater.LauncherUpdaterMain %*
