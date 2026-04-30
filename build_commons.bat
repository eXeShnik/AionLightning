@echo off
SET "JAVA_HOME=C:\Program Files\Java\jdk-1.8"
SET "PATH=%JAVA_HOME%\bin;%PATH%"

pushd AL-Commons
call ..\Tools\Ant\bin\ant
if errorlevel 1 (
	popd
	exit /b %errorlevel%
)
popd

copy /Y "AL-Commons\build\al-commons.jar" "AL-Login\libs\"
if errorlevel 1 exit /b %errorlevel%
copy /Y "AL-Commons\build\al-commons.jar" "AL-Game\libs\"
if errorlevel 1 exit /b %errorlevel%
copy /Y "AL-Commons\build\al-commons.jar" "AL-Chat\libs\"
if errorlevel 1 exit /b %errorlevel%