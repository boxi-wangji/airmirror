# AirMirror

把 iPhone 画面投到 Windows。

[English README](./README.en.md)

不录屏，不保存画面，不上传画面。

## 使用

1. 运行 `AirMirror-Setup-*.exe` 安装 AirMirror。
2. 从开始菜单打开 AirMirror，填写设备名。
3. 点击「开始接收 AirPlay」。
4. 在 iPhone 控制中心打开「屏幕镜像」，选择该设备名。

iPhone 与电脑需要连接同一 Wi‑Fi。公司、酒店或访客 Wi‑Fi 若开启设备隔离，iPhone 将无法找到电脑。

## AirMirror 做什么

- 接收 iPhone 的 AirPlay 屏幕镜像。
- 使用 Direct3D 12 显示画面。
- 使用未修改的 UxPlay Windows 接收内核。

## 安装与卸载

安装包会创建桌面和开始菜单快捷方式；可在 Windows 设置中卸载。

首次启动时，Windows 可能提示 Bonjour 或网络访问权限，按系统提示处理即可。

## 从源码构建

需要 Windows 和 .NET SDK 10。

```powershell
.\installer\Build-Installer.ps1 -Version 1.0.0
```

构建脚本会下载并校验 UxPlay Windows 接收内核，生成安装包到 `构建\安装程序\`。源码和构建产物分开：`源码\` 进入 Git，`构建\` 不进入 Git。

## 许可

AirMirror 采用 [GPL-3.0](./LICENSE) 许可证。第三方内核的来源和许可证见 [第三方许可.md](./第三方许可.md)。
