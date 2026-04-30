@echo off
SET "JAVA_HOME=C:\Program Files\Java\jdk-1.8"
SET "PATH=%JAVA_HOME%\bin;%PATH%"

pushd AL-Game
call ..\Tools\Ant\bin\ant
set "BUILD_ERROR=%ERRORLEVEL%"
popd
exit /b %BUILD_ERROR%
