@echo off

ECHO Building all servers...
ECHO ============================

ECHO Building Commons...
call build_commons.bat
if errorlevel 1 exit /b %errorlevel%
ECHO ============================

ECHO Building LoginServer...
call build_loginserver.bat
if errorlevel 1 exit /b %errorlevel%
ECHO ============================

ECHO Building GameServer...
call build_gameserver.bat
if errorlevel 1 exit /b %errorlevel%
ECHO ============================

ECHO Building ChatServer...
call build_chatserver.bat
if errorlevel 1 exit /b %errorlevel%

if not exist "JavaServerBuild" mkdir "JavaServerBuild"
MOVE /Y "AL-Login\build\dist\AL-Login" "JavaServerBuild"
MOVE /Y "AL-Game\build\dist\AL-Game" "JavaServerBuild"
MOVE /Y "AL-Chat\build\dist\AL-Chat" "JavaServerBuild"

ECHO ============================
ECHO Build completed successfully.