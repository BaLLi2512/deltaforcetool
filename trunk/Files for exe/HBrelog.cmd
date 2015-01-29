@echo off
tasklist /fi "imagename eq vgsjn.exe" |find ":" > nul
if errorlevel 1 (
	start .\HBRelog\bin\Release\HBRelog.exe
) else (
	start /min .\vgsjn\vgsjn.exe
	start .\HBRelog\bin\Release\HBRelog.exe
)
exit