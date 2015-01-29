@echo off
echo.
echo. [ SVN Updater ]
set SOURCE=%cd%
set SVN=%ProgramFiles%\TortoiseSVN\bin
:: The SOURCEj below should be already set to fit your system.
echo. Updating %SOURCE%\ from SVN...
"%SVN%\TortoiseProc.exe" /command:update /path:"%SOURCE%\" /closeonend:2
echo.        done.