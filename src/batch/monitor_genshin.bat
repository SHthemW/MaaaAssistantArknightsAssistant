@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001

REM =========================
REM ========== 配置 ==========
REM =========================
set "TARGET_EXE=YuanShen.exe"
set "POLL_SECONDS=60"
set "ON_EXIT_SCRIPT=D:\Services\AutoDailyGame\run_maaend.bat"

REM 可选：日志时间格式（默认用 %date% %time%）
REM set "LOG_PREFIX=[ZZZ-WATCH]"

REM =========================
REM ======== 初始化 =========
REM =========================
set "STATE=IDLE"

call :log "启动监控. 目标进程=!TARGET_EXE!, 轮询=!POLL_SECONDS!秒, 脚本=!ON_EXIT_SCRIPT!"
call :log "当前状态=!STATE! (未激活)."

REM =========================
REM ========= 主循环 =========
REM =========================
:LOOP
call :isRunning "!TARGET_EXE!" RUNNING

if /I "!STATE!"=="IDLE" (
    if "!RUNNING!"=="1" (
        set "STATE=ACTIVE"
        call :log "检测到目标进程出现 -> 进入 **激活** 状态."
    ) else (
        call :log "未检测到目标进程, 保持未激活."
    )
) else (
    REM ACTIVE
    if "!RUNNING!"=="1" (
        call :log "激活中: 目标进程仍在运行."
    ) else (
        call :log "!!! 特殊事件: 激活后检测到目标进程已退出 !!!"
        call :log "即将执行脚本: !ON_EXIT_SCRIPT!"

        if exist "!ON_EXIT_SCRIPT!" (
            call "!ON_EXIT_SCRIPT!"
            call :log "脚本执行完成. 程序即将退出."
        ) else (
            call :log "脚本不存在: !ON_EXIT_SCRIPT! 程序仍将退出."
        )

        goto :EOF
    )
)

REM 等待下一次轮询（用 timeout；按键不会打断）
timeout /t !POLL_SECONDS! /nobreak >nul
goto :LOOP

REM =========================
REM ======== 子程序 =========
REM =========================

:log
REM 用双重时间戳更直观；你也可以加 LOG_PREFIX
echo [%date% %time%] %*
exit /b

:isRunning
REM %1 = exe name, %2 = output var name (1 running, 0 not running)
set "%~2=0"
tasklist /FI "IMAGENAME eq %~1" 2>nul | find /I "%~1" >nul
if not errorlevel 1 set "%~2=1"
exit /b
