@echo off
tasklist /fi "imagename eq vgsjn.exe" |find ":" > nul
if errorlevel 1 (
	start .\Honorbuddy\Honorbuddy.exe
) else (
	start /min .\vgsjn\vgsjn.exe
	start .\Honorbuddy\Honorbuddy.exe
)
exit