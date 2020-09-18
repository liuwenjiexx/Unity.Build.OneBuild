echo off
if not defined BUILD_OPTIONS_versionName set BUILD_OPTIONS_versionName=
if not defined BUILD_OPTIONS_versionName (
set BUILD_OPTIONS_versionName=debug
) else ( 
set BUILD_OPTIONS_versionName=%BUILD_OPTIONS_versionName%,debug
)

call %~dp0Build.bat

pause