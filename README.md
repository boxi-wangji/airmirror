# AirMirror

给 iPhone 用的极简 AirPlay 接收器。

它只做一件事：

`iPhone → AirPlay → 这台 Windows 电脑`

不录屏，不保存画面，不上传画面。

## 怎么用

1. 运行 `AirMirror-Setup-*.exe` 完成安装，然后从开始菜单打开 AirMirror。
2. 填一个设备名，例如 `波西投屏`。
3. 点击「开始接收 AirPlay」。
4. iPhone 和电脑连接同一 Wi‑Fi。
5. iPhone 打开控制中心 → 屏幕镜像 → 选择这个设备名。

若 Windows 首次出现 Bonjour 或网络访问的系统提示，按系统提示处理即可。安装成功后，接收内核会自动重启一次。

AirMirror 当前固定使用 Direct3D 12 输出画面，并关闭了音视频同步丢帧，避免首帧被错误跳过。

## 文件结构

- `src/`：AirMirror 启动器源码。
- `engine/`：开源 UxPlay Windows 接收内核及其视频、音频、网络组件；安装包会将其部署到程序目录。
- `%LOCALAPPDATA%\\AirMirror\\`：AirMirror 的本机设备名，第一次运行后生成。
- `%LOCALAPPDATA%\\AirMirror\\logs\\gstreamer.log`：仅在排查画面故障时生成的本机视频日志。
- `installer/`：Inno Setup 安装包脚本与构建脚本。

## 安装包

从 GitHub Releases 下载 `AirMirror-Setup-*.exe`，运行后即可安装、创建开始菜单快捷方式，并可从 Windows 设置中卸载。

## 从源码构建

需要 Windows、.NET SDK 10 和 Inno Setup 6。以下命令会下载并校验指定版本的 UxPlay Windows 接收内核，再生成安装包：

```powershell
.\installer\Build-Installer.ps1 -Version 1.0.0
```

输出文件为 `dist\AirMirror-Setup-1.0.0.exe`。

## 不做的事

- 不使用 AirServer 的代码。
- 不绕过 AirServer 试用限制。
- 不录制画面。
- 不控制 iPhone。

## 网络要求

- iPhone 与电脑必须在同一局域网。
- 公司、酒店、访客 Wi‑Fi 若开启“设备隔离”，会导致 iPhone 找不到电脑。

## 第三方内核

AirMirror 启动的是未修改的 `uxplay-windows` 接收内核。其许可证和来源见 [第三方许可.md](./第三方许可.md)。
