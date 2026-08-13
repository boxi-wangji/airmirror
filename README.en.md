# AirMirror

Mirror your iPhone screen to Windows.

[中文说明](./README.md)

No recording. No local captures. No uploads.

## Get started

1. Run `AirMirror-Setup-*.exe` to install AirMirror.
2. Open AirMirror from the Start menu and enter a device name.
3. Select “Start receiving AirPlay”.
4. Open “Screen Mirroring” in iPhone Control Center and select the device name.

Your iPhone and PC must use the same Wi‑Fi network. Networks with client isolation may prevent the iPhone from finding the PC.

## What AirMirror does

- Receives AirPlay screen mirroring from iPhone.
- Renders video with Direct3D 12.
- Uses the unmodified UxPlay Windows receiver engine.

## Install and uninstall

The installer creates Desktop and Start menu shortcuts. You can uninstall it from Windows Settings.

On first launch, Windows may ask for Bonjour or network access permission. Follow the system prompt.

## Build from source

Windows and .NET SDK 10 are required.

```powershell
.\installer\Build-Installer.ps1 -Version 1.0.0
```

The build script downloads and verifies the UxPlay Windows receiver engine, then writes the installer to `构建\安装程序\`. Source and build output are separate: `源码\` is tracked by Git, while `构建\` is not.

## License

AirMirror is licensed under [GPL-3.0](./LICENSE). See [第三方许可.md](./第三方许可.md) for the third-party engine source and license.
