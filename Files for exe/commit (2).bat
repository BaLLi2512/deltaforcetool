@echo off
echo.
echo. [ SVN Committer ]
:: The two lines below should be changed to suit your system.
set SOURCE=D:\SVN\deltaforcetool\
set SVN=D:\Program Files\TortoiseSVN\bin
echo.
echo. Committing %SOURCE% to SVN...
"%SVN%\TortoiseProc.exe" /command:commit /path:"%SOURCE%" /logmsg:"DeltaForceTechs FTW!" 
echo. done.
echo.
echo. Operation complete.