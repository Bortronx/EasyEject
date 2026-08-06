# EasyEject

Safely eject external storage devices on Windows with one click.

EasyEject is a lightweight WinUI 3 desktop application built with .NET 9 and the Windows App SDK 1.8. It detects all connected USB flash drives, external hard drives, and memory cards, then safely ejects them using the Windows Plug and Play API -- no administrator privileges required.

## Features

- **Eject All External Devices** -- safely removes every removable storage device with a single click.
- **Eject Selected Device** -- eject a specific device via radio button selection.
- Live hot-plug detection -- the device list refreshes automatically when devices are connected or removed.
- Displays drive letters, capacity, file system, and volume label for each device.
- Light / Dark / System theme switcher.
- Progress dialog with per-device status during bulk ejection.
- File logging via Serilog for troubleshooting.

## Requirements

- **Windows 10 1809+** (Windows App SDK 1.8 / WinUI 3)
- **.NET 9 SDK** to build from source
- Sideload-enabled or Developer Mode enabled for MSIX installation

## Quick start

```powershell
# Clone the repository
git clone https://github.com/easyeject/easyeject.git
cd easyeject

# Build (Release, x64)
dotnet build .\src\EasyEject.UI\EasyEject.UI.csproj -c Release

# Run tests (49 tests)
dotnet test .\src\EasyEject.Tests\EasyEject.Tests.csproj

# Build + sign the MSIX installer
powershell -ExecutionPolicy Bypass -File .\src\EasyEject.Package\build-msix.ps1

# Install the MSIX
powershell -ExecutionPolicy Bypass -File .\src\EasyEject.Package\install-msix.ps1
```

## Deliverables

| Artifact | Path | Notes |
| --- | --- | --- |
| Sideload MSIX (x64) | `src\EasyEject.Package\bin\EasyEject-x64.msix` | Self-signed, self-contained (includes WinAppSDK 1.8 runtime). |
| Single-file EXE (x64) | `src\EasyEject.UI\bin\Release\...\publish\EasyEject.exe` | Self-contained, untrimmed. |

## How it works

1. **Detection** -- Enumerates USB storage devices via the Windows SetupAPI and maps volumes through WMI (`Win32_DiskDrive`, `Win32_LogicalDiskToPartition`) and the `FindFirstVolume` API. All calls are accessible to standard (non-elevated) users.
2. **Eject** -- Locks and dismounts each volume, then calls `CM_Request_Device_EjectW` via the Plug and Play manager. When the disk child is vetoed, the app walks up the device tree and ejects the USB parent device, mimicking the Windows "Safely Remove Hardware" behavior.
3. **Retry** -- Busy or vetoed devices are retried with short delays to allow handles to close.

## Solution layout

| Project | Purpose |
| --- | --- |
| `EasyEject.Models` | Domain models (device info, eject results). |
| `EasyEject.Win32` | Win32 interop: SetupAPI, CFGMGR32, kernel32 P/Invoke declarations. |
| `EasyEject.Core` | Business logic interfaces and services (eject orchestration, settings). |
| `EasyEject.Services` | Windows implementations (device enumeration, safe eject, settings storage). |
| `EasyEject.UI` | WinUI 3 app: MVVM (CommunityToolkit.Mvvm), pages, converters, DI. |
| `EasyEject.Package` | MSIX packaging scripts (`build-msix.ps1`, `install-msix.ps1`, manifest). |
| `EasyEject.Tests` | xUnit tests (eject coordinator, device classifier, capacity formatting). |

## Documentation

- [Build guide](docs/BUILD.md) -- detailed build, test, publish, and install instructions.
- [Packaging notes](docs/PACKAGING.md) -- MSIX/WinUI packaging pitfalls and how this repo handles them.

## Logs

Serilog writes to `%LOCALAPPDATA%\EasyEject\Logs\easyeject-*.log` (rolling daily, 14 files retained).

## License

MIT -- see [LICENSE](LICENSE).
