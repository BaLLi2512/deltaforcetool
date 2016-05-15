@echo off
tasklist /FI "IMAGENAME eq vgsjn.exe" 2>NUL | find /I /N "vgsjn.exe">NUL
if "%ERRORLEVEL%"=="0" (
goto withoutvgsjn
) else (
goto normal )

:normal
cd vgsjn
start vgsjn.exe
cd ..
cd Honorbuddy
start Honorbuddy.exe
exit

:withoutvgsjn
cd Honorbuddy
start Honorbuddy.exe
exit