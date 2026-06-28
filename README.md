# Maaa Assistant Arknights Assistant (MAAA)

基于 WPF 的游戏日常任务统一启动器，用来集中编排多款游戏自动化工具的启动、监控、通知与收尾流程。

[CHANGELOG](CHANGELOG.md)

<p align="center">
  <img src="res/icon.ico" width="20%" />
</p>

<p align="center">
  <img src="res/mainui.png" width="70%" />
</p>

## 主要能力

- 多任务链顺序执行，前一项任务结束后自动进入下一项。
- 图形界面集中管理任务启用状态、启动参数、监控进程、延迟和超时时间。
- 支持单独启动某一项任务，也支持一键启动整条任务链。
- 支持登录后自动运行，并可限制在指定时间窗内启动。
- 支持时间窗内随机时刻自动启动，降低固定时点触发的干扰。
- 支持启动时自动静音，并在任务结束后恢复原始音量状态。
- 支持通过 Twinkle Tray 将显示器亮度调至最低，并在结束后恢复原亮度。
- 支持任务完成后自动关机，并可限制为仅自动运行时生效。
- 支持运行日志落盘、界面日志导出、清空与自动滚动。
- 支持 Webhook 推送、自定义推送模板、按日志类别筛选推送内容。
- 支持本地 Webhook 中转，将本地地址映射转发到原始 Webhook 地址。
- 支持调用智谱 AI 对最近一次运行结果做总结，并支持超时、重试、流式返回、深度思考和提示词配置。
- 支持复用最近一次 AI 总结提示日志进行测试，便于调试总结效果。

## 支持的游戏与工具

| 游戏 | 自动化工具 |
| --- | --- |
| 明日方舟 | [MAA](https://github.com/MaaAssistantArknights/MaaAssistantArknights) |
| 崩坏：星穹铁道 | [March7th Assistant](https://github.com/moesnow/March7thAssistant) |
| 绝区零 | [ZenlessZoneZero-OneDragon](https://github.com/DoctorReid/ZenlessZoneZero-OneDragon) |
| 原神 | [BetterGI](https://github.com/babalae/better-genshin-impact) |
| 鸣潮 | [ok-ww](https://github.com/ok-oldking/ok-ww) |
| MaaEnd | 本仓库内置批处理任务 |

## 安装与运行

### 直接使用

从 [Releases](https://github.com/SHthemW/MaaaAssistantArknightsAssistant/releases) 下载最新版本，解压后运行主程序即可。

### 首次启动

程序首次启动会在可执行文件同目录生成 `appsettings.Local.json`，后续配置会自动保存到该文件。

### 自动运行

- 在界面中勾选“开机自动启动”后，程序会通过 Windows 计划任务注册当前实例的登录自启动项。
- 自动运行模式会以 `--autorun` 参数启动。
- 如果当前时间不在允许的自动启动时间窗内，程序会静默退出，不弹出主界面。

## 配置说明

### 任务配置

每个任务都可以在界面中单独配置：

- 启动文件路径或 URI。
- 启动参数。
- 需要监控的游戏进程名。
- 启动前延迟时间。
- 超时时间。
- 是否启用。

其中原神默认支持通过 `bettergi://` URI 方式启动，使用前需要本机已正确安装并注册 BetterGI 协议。

### 防打扰配置

- 启动时静音。
- 仅自动运行时静音。
- 结束后恢复音量。
- 启动时调低亮度。
- 仅自动运行时调低亮度。
- 结束后恢复亮度。
- 全部任务完成后关机。
- 仅自动运行时关机。

亮度控制依赖本机正在运行的 Twinkle Tray。

### 调度配置

- 自动运行时间窗起止时间。
- 轮询间隔。
- 时间窗内随机启动。

### Webhook 配置

- 普通 Webhook 推送 URL。
- 自定义请求体模板，支持 `__TIME__` 和 `__CONTENT__` 占位符。
- 仅推送 AI 总结结果。
- 按日志分类选择推送内容。
- Webhook 中转端口。
- Webhook 中转原始 URL。

Webhook 中转会监听本地端口，并按照“原始 URL 的路径和查询参数”进行映射转发。

### AI 总结配置

当前已接入智谱 AI，总结配置支持：

- API Key。
- API URL。
- 模型名。
- 系统提示词。
- 总结提示词。
- Temperature。
- 超时秒数。
- 重试次数。
- 是否启用深度思考。
- 是否启用流式返回。
- 是否仅在自动运行时生成总结。

## 日志与文件

- 运行日志会写入 `logs/session-*.log`。
- AI 总结提示日志会写入 `logs/aiprompt-*.log`。
- 运行日志和 AI 提示日志默认保留 5 天。
- 界面导出的日志会写入 `logs/runtime-{yyyyMMdd-HHmmss}.log`。

## 开发

### 环境要求

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 构建

```bash
dotnet build
```

### 运行

```bash
dotnet run
```

### 发布

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

发布完成后会自动压缩输出目录，并生成到 `bin/Release-Archives/`，文件名格式为 `MaaaAssistantArknightsAssistant-{RID}-{yyyyMMdd-HHmm}.zip`。

## 项目结构

```text
.
|-- Game-Daily-Routine-Launcher.csproj
|-- res/
|   |-- icon.ico
|   `-- mainui.png
`-- src/
    |-- App.xaml
    |-- MainWindow.xaml
    |-- Models/
    |-- Services/
    |-- ViewModels/
    |-- Converters/
    |-- batch/
    `-- WebhookRelayHelpWindow.xaml
```

`src/batch/` 中保留了原有批处理脚本，便于兼容既有工具链与独立脚本调用。
