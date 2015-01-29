
echo.
echo. [ SVN Committer ]
:: The two lines below should be changed to suit your system.
set SOURCE=D:\SVN\deltaforcetool\
set SVN=D:\Program Files\Subversion\bin
echo.
echo. Committing %SOURCE% to SVN...
"%SVN%\svn.exe" add * %SOURCE%
"%SVN%\svn.exe" commit %SOURCE% 
echo. done.
echo.
echo. Operation complete.