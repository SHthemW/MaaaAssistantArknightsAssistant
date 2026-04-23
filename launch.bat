@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001

rem ======= 可配置项（按需修改） =======
set "D=D:\Services\March7thAssistant_full\March7thAssistant_full\March7th Assistant.exe"
set "E=D:\Services\MAA-v5.16.5-win-x64\MAA.exe"
rem ===================================

nircmd setsysvolume 0

rem 启动程序
start "" "%E%"
timeout /t 30
start "" "%D%"
exit /b 0
