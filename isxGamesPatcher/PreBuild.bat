@ECHO OFF
:: delims is a TAB followed by a space
FOR /F "tokens=2* delims=	 " %%A IN ('REG QUERY "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\InnerSpace.exe" /v "Path"') DO SET InnerSpacePath=%%B

echo Copying "%InnerSpacePath%\.NET Programs\ISXVGWrapper.dll" to %1
copy /Y "%InnerSpacePath%\.NET Programs\ISXVGWrapper.dll" %1

echo Copying "%InnerSpacePath%\Lavish.InnerSpace.dll" to %1
copy /Y "%InnerSpacePath%\Lavish.InnerSpace.dll" %1
