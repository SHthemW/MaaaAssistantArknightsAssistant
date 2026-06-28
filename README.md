<div align="center">
  <img src="res/icon.ico" width="25%" alt="MAAA icon" />
  <h1>Maaa Assistant Arknights Assistant</h1>
  <p>
    <img src="https://img.shields.io/github/downloads/SHthemW/MaaEnd-Webhook-Retransmitter/total" alt="downloads" />
  </p>
  <p>
    <a href="CHANGELOG.md">CHANGELOG</a>
  </p>
  <p>
    游戏日常助手Hub, 可用来集中编排多款游戏自动化工具的启动、监控、通知与收尾流程.
  </p>
  <p>
    本程序和 <a href="https://github.com/MaaAssistantArknights/MaaAssistantArknights">MAA (MaaAssistantArknights)</a> 没有直接关联, 但很适合搭配使用.
  </p>
  <p>
    除了链式启动, MAAA还集成了许多实用的自动化功能, 让你可以每天几乎无需消耗心智在无聊的日常上.
  </p>
  <p>
    <em>可能是你用过的最好的助手编排器 !</em>
  </p>
  <h1></h1>
</div>



除了启动各个助手, 本程序还提供以下全面且实用的功能:
- **开机静默运行**:

  支持活跃时间配置. 在非活跃时间段不产生任何影响, 在活跃时间段内开工! 

  搭配米家智能插座+BIOS来电自启设置, 在你睡觉的时候静默开启一条龙, 完成任务后自动关机, 醒来时已然清新无负担🌿.

<p align="center">
  <img src="res/test_video.gif" width="30%" />
</p>



- **多重防打扰:** 

  活跃时间内自动静音🔕+调低屏幕亮度☀️(需搭配Twinkle Tray), 不打扰正在熟睡的你.

  特别支持计划任务高优先级启动, 把开机时其他程序的提示音也扼杀在摇篮中.

  <p align="center">
    <img src="res/fn_mute.png" width="70%" />
  </p>

  

- **完善的过程链和保底机制:**

  从进程层面监控每个助手的运行情况, 实现**无人托管的自动链式运行**.

  具有**超时保底**机制, 如果某个游戏因为需要更新/助手内部错误等问题无法运行, 也不会影响其它助手的功能.

  

- **额外的Webhook推送和中转**:

  MAAA单独维护了一套Webhook推送功能, 对一些不支持Webhook的助手友好, 让你至少能得知对应助手的运行状态.

  支持**Webhook中转服务**. 将你原本的推送桥接到MAAA上, 不仅能实现原本的推送功能, 还能让MAAA得知更多助手内部状态, 用于接下来的AI总结.

  <p align="center">
    <img src="res/fn_webhook.png" width="60%" />
  </p>

- **AI智能总结**:

  通过接入外部的免费大模型, 对助手们今天的工作情况做汇总, 让你无需花费经历二次验收.

  <p align="center">
    <img src="res/fn_aip.png" width="66%" />
    <img src="res/fn_ai.jpg" width="20%" />
  </p>



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

- 启动文件路径或 URL。
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
