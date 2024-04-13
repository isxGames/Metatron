@ECHO OFF
:: delims is a TAB followed by a space
FOR /F "tokens=2* delims=	 " %%A IN ('REG QUERY "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\InnerSpace.exe" /v "Path"') DO SET InnerSpacePath=%%B

echo Copying %1 to "%InnerSpacePath%\.NET Programs"
copy /Y %1 "%InnerSpacePath%\.NET Programs"