@echo off
:: Variablen setzen
set root="%cd%"
set update="%root%\update"
set settingsdir="%root%\Honorbuddy\Settings"
set tmp="%root%\TEMP"
cls
echo.
echo                      !!! ACHTUNG !!!
echo.
echo Bitte sicherstellen, dass die Prozesse fuer HBRelog, Honorbuddy, 
echo             WoW und CD Patcher beendet sind.
echo.
echo Zum Fortfahren
pause
cd update\wget\bin
wget --no-check-certificate https://github.com/BaLLi2512/deltaforcetool/archive/master.zip
move %update%\wget\bin\master %root%\deltaforcetool-master.zip
cls
echo.
echo Um mit dem Update zu beginnen
pause
:: noetige Ordner anlegen
mkdir TEMP
mkdir TEMP\Settings
:: Sicherung der vorhandenen Settings
xcopy %settingsdir% %tmp%\Settings /E /Q /H
:: entfernen alter Software
rd /S /Q %root%\HBRelog
rd /S /Q %root%\Honorbuddy
rd /S /Q "%root%\usefull Stuff"
rd /S /Q %root%\vgsjn
del /f /s /q %root%\Honorbuddy.exe
del /f /s /q %root%\HBRelog.exe
del /f /s /q %root%\Honorbuddy.exe
del /f /s /q "%root%\READ b4 USE.txt"
:: Installation neuer Software
%update%\7Zip\7z.exe x %root%\deltaforcetool-master.zip -o%tmp%
cd /d %tmp%\deltaforcetool-master
for %%i in (*) do move "%%i" %root%
for /d %%i in (*) do move "%%i" %root%
mkdir %root%\Honorbuddy\Settings
:: Restore der Sicherung
cd /d %tmp%"\Settings
for %%i in (*) do move "%%i" %settingsdir%
for /d %%i in (*) do move "%%i" %settingsdir%
:: Aufraeumen
cd %root%
rd /S /Q TEMP
del /f /s /q deltaforcetool-master.zip
cls
echo.
echo Update erfolgreich ausgefuehrt.
echo Zum Bennden
pause
exit