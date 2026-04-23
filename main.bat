@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001

set "HH=%time:~0,2%"
set "HH=%HH: =0%"

rem 转为数值以便比较（如 "04" -> 4）
set /a H=1%HH% - 100

if %H% GEQ 4 if %H% LSS 5 (
    echo [%date% %time%] 在 04:00-04:59 之间，开始执行任务...

    start D:\Services\AutoDailyGame\launch.bat
) else (
    nircmd setsysvolume 16384
)

exit /b 0
