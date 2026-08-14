# Third-party notices

AirMirror’s interface and launcher code were written independently. It does not include AirServer code.

## UxPlay Windows receiver engine

- Downloaded during the build to `构建/运行环境/engine/uxplay-windows.exe` and its adjacent runtime files
- Version: `uxplay-windows 2.0.0.1736`
- Release source: [leapbtw/uxplay-windows release](https://github.com/leapbtw/uxplay-windows/releases/tag/2.0.0.1736)
- Release archive: `uxplay-windows.zip`
- SHA-256: `9d3a51c15fc9db857351195e7eb7bbb21700d9ae25d936a54bcf8536b62cca18`
- License: GPL-3.0. The original license file is `engine/LICENSE.rtf` in the build runtime.

The receiver engine uses these upstream projects:

- [FDH2/UxPlay](https://github.com/FDH2/UxPlay)
- Apple mDNSResponder
- GStreamer and its components

Before distributing a work containing this engine, comply with GPL-3.0 and the licenses of its components.
