# AirMirror

给 iPhone 用的极简 AirPlay 接收器。

它只做一件事：

`iPhone → AirPlay → 这台 Windows 电脑`

不录屏，不保存画面，不上传画面。

## 怎么用

1. 运行 `AirMirror-Setup-*.exe` 完成安装，然后从开始菜单打开 AirMirror。
2. 填一个设备名，例如 `波西投屏`。首次安装默认留空，由你自行命名。
3. 点击「开始接收 AirPlay」。
4. iPhone 和电脑连接同一 Wi‑Fi。
5. iPhone 打开控制中心 → 屏幕镜像 → 选择这个设备名。

若 Windows 首次出现 Bonjour 或网络访问的系统提示，按系统提示处理即可。安装成功后，接收内核会自动重启一次。

AirMirror 当前固定使用 Direct3D 12 输出画面，并关闭了音视频同步丢帧，避免首帧被错误跳过。

## 文件结构

- `src/`：AirMirror 启动器源码。
- `assets/`：AirMirror 的 SVG Logo，以及生成 Windows 图标和安装向导视觉素材的脚本。
- `installer/`：简体中文安装向导与构建脚本。
- `dist/`：只保留最新构建的安装包。
- `engine/`、`artifacts/`、`src/bin/` 和 `src/obj/`：构建过程的临时内容，均由脚本自动生成，不纳入源码。

## 安装包

运行 `AirMirror-Setup-*.exe`，即可通过简体中文安装向导完成安装、创建开始菜单快捷方式，并可从 Windows 设置中卸载。

## 从源码构建

需要 Windows、.NET SDK 10 和 Inno Setup 6。以下命令会下载并校验指定版本的 UxPlay Windows 接收内核，再生成安装包：

```powershell
.\installer\Build-Installer.ps1 -Version 1.0.0
```

输出文件为 `dist\AirMirror-Setup-1.0.0.exe`；重新构建时会自动清理旧安装包。

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
