@echo off
echo.
echo. [ SVN Committer ]
:: The two lines below should be changed to suit your system.
set SOURCE=D:\SVN\deltaforcetool
set SVN=D:\Program Files\Subversion\bin\svn.exe
echo.
echo. Committing %SOURCE% to SVN...
"%SVN%" add * %SOURCE%
"%SVN%" commit -m "test" %SOURCE%
echo. done.
echo.
echo. Operation complete.