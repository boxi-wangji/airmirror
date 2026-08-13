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

项目根目录分为两个目录：

- `源码/`：Git 仓库。包含 `src/`、`assets/`、`installer/`、README 和许可证。
- `构建/`：不进 Git。包含 `安装程序/`、`程序文件/`、`运行环境/` 和临时文件。

SVG 是 Logo 的唯一正式源文件；ICO 和安装向导图片由构建脚本生成到 `构建/临时/品牌素材/`。

## 安装包

运行 `AirMirror-Setup-*.exe`，即可完成一键安装、创建桌面和开始菜单快捷方式，并可从 Windows 设置中卸载。

## 从源码构建

需要 Windows 和 .NET SDK 10。以下命令会下载并校验指定版本的 UxPlay Windows 接收内核，再生成 Velopack 一键安装包：

```powershell
.\installer\Build-Installer.ps1 -Version 1.0.0
```

输出文件为 `构建\安装程序\AirMirror-Setup-1.0.0.exe`；重新构建时会自动清理旧安装包。安装过程使用 AirMirror 品牌画面，不使用传统安装向导。

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
