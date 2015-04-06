@echo off
:start
tasklist /FI "IMAGENAME eq vgsjn.exe" 2>NUL | find /I /N "vgsjn.exe">NUL
if "%ERRORLEVEL%"=="0" (
goto warning1
) else (
goto checkHB )
:checkHB
tasklist /FI "IMAGENAME eq Honorbuddy.exe" 2>NUL | find /I /N "Honorbuddy.exe">NUL
if "%ERRORLEVEL%"=="0" (
goto warning2
) else (
goto checkHBR )
:checkHBR
tasklist /FI "IMAGENAME eq HBRelog.exe" 2>NUL | find /I /N "HBRelog.exe">NUL
if "%ERRORLEVEL%"=="0" (
goto warning3
) else (
goto update )

:warning1
cls
echo.
echo                      !!! ACHTUNG !!!
echo.
echo Bitte sicherstellen, dass der Prozess fuer CD Patcher beendet ist.
echo.
echo Zum Fortfahren
pause
goto start

:warning2
cls
echo.
echo                      !!! ACHTUNG !!!
echo.
echo Bitte sicherstellen, dass der Prozess fuer Honorbuddy beendet ist.
echo.
echo Zum Fortfahren
pause
goto start

:warning3
cls
echo.
echo                      !!! ACHTUNG !!!
echo.
echo Bitte sicherstellen, dass der Prozess fuer HBRelog beendet ist.
echo.
echo Zum Fortfahren
pause
goto start

:update
:: Variablen setzen
set root="%cd%"
set update="%root%\update"
set settingsdir="%root%\Honorbuddy\Settings"
set tmp="%root%\TEMP"
cd update\wget\bin
wget --no-check-certificate https://github.com/BaLLi2512/deltaforcetool/archive/master.zip
move %update%\wget\bin\master %root%\deltaforcetool-master.zip
cls
echo.
echo Ich hab das Zeug! Jetzt wird es ernst!
echo.
echo LETS GETTY TO RAMBO!
echo.
pause
:: noetige Ordner anlegen
mkdir %root%\TEMP
mkdir %root%\TEMP\Settings
:: Sicherung der vorhandenen Settings
xcopy %settingsdir% %tmp%\Settings /E /Q /H
:: entfernen alter Software
rd /S /Q %root%\HBRelog
rd /S /Q %root%\Honorbuddy
rd /S /Q "%root%\usefull Stuff"
rd /S /Q %root%\vgsjn
::rd /S /Q "%localappdata%\Bossland\Buddy Store"
del /f /s /q %root%\Honorbuddy.exe
del /f /s /q %root%\HBRelog.exe
del /f /s /q %root%\Honorbuddy.exe
del /f /s /q "%root%\READ b4 USE.txt"
:: Installation neuer Software
%update%\7Zip\7z.exe x %root%\deltaforcetool-master.zip -o%root%
cd /d %root%\deltaforcetool-master
for %%i in (*) do move "%%i" %root%
for /d %%i in (*) do move "%%i" %root%
mkdir %root%\Honorbuddy\Settings
:: Restore der Sicherung
cd /d %tmp%"\Settings
for %%i in (*) do move "%%i" %settingsdir%
for /d %%i in (*) do move "%%i" %settingsdir%
cls
echo.
echo Schwein gehabt - Update erfolgreich ausgefuehrt. Bitte ziehe in Betracht, 
echo den BuddyWizard unter usefull Stuff zusaetzlich auszufuehren.
echo.
echo Falls der HB beim Start haengen bleibt, loesche bitte den 
echo Buddy Store-Ordner in den 'LocalAppData'.
echo Zum Aufraeumen und Bennden
pause
:: Aufraeumen
cd %root%
rd /S /Q TEMP
rd /S /Q deltaforcetool-master
del /f /s /q deltaforcetool-master.zip
::start ".\usefull Stuff\BuddyWizard.exe"