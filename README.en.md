<p align="center">
  <img src="./assets/airmirror-logo.svg" width="144" alt="AirMirror Logo">
</p>

<h1 align="center">AirMirror</h1>

<p align="center">
  Mirror your iPhone screen to Windows<br>
  Simple · Local · Focused
</p>

<p align="center">
  <img src="https://img.shields.io/badge/version-1.0.5-12D8F2?style=flat-square" alt="Version 1.0.5">
  <img src="https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-153557?style=flat-square" alt="Windows 10 and 11">
  <img src="https://img.shields.io/badge/license-GPL--3.0-D7A06B?style=flat-square" alt="GPL-3.0">
</p>

<p align="center">
  <a href="./README.md">Chinese</a> · <strong>English</strong>
</p>

---

## Overview

AirMirror is a Windows AirPlay receiver for iPhone. It does one thing: mirror your screen to this PC. No recording. No local captures. No uploads.

## Get started

1. Run `AirMirror-Setup-*.exe` to install AirMirror.
2. Open AirMirror from the Start menu and enter a device name.
3. Select “Start receiving AirPlay”.
4. Open “Screen Mirroring” in iPhone Control Center and select the device name.

> Your iPhone and PC must use the same Wi‑Fi network. Networks with client isolation may prevent the iPhone from finding the PC.

## Install and uninstall

- The installer creates Desktop and Start menu shortcuts.
- You can uninstall it from Windows Settings.
- On first launch, Windows may ask for Bonjour or network access permission. Follow the system prompt.

## Build from source

Windows and .NET SDK 10 are required.

```powershell
.\installer\Build-Installer.ps1 -Version 1.0.0
```

The installer is written to the project build directory. The source directory is tracked by Git; build output is not.

## License

AirMirror is licensed under [GPL-3.0](./LICENSE). See [Third-party notices](./第三方许可.md) for the third-party engine source and license.
