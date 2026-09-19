# Building, testing, and shipping EasyEject

## Prerequisites

- Windows 10 1809+ with Developer Mode or sideloading enabled for MSIX installs.
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).
- Windows SDK 10.0.26100 (for `makeappx.exe` / `signtool.exe` at
  `C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\`). Update the
  paths in `src\EasyEject.Package\build-msix.ps1` if you use another version.

## Build

```powershell
# Release build (x64 is the default Platform and RuntimeIdentifier)
dotnet build .\src\EasyEject.UI\EasyEject.UI.csproj -c Release
```

Output: `src\EasyEject.UI\bin\Release\net9.0-windows10.0.19041.0\win-x64\EasyEject.exe`
(self-contained; the WinAppSDK runtime is deployed next to the exe).

## Test

```powershell
dotnet test .\src\EasyEject.Tests\EasyEject.Tests.csproj
```

## Publish the single-file EXE

```powershell
dotnet publish .\src\EasyEject.UI\EasyEject.UI.csproj -c Release -r win-x64
```

Output: `src\EasyEject.UI\bin\Release\net9.0-windows10.0.19041.0\win-x64\publish\EasyEject.exe`

Notes:

- Trimming is disabled on purpose (`PublishTrimmed=false` in the csproj).
  The .NET trimmer strips the CsWinRT interface marshaling metadata used by
  WinUI 3 XAML bindings, causing a `NullReferenceException` in
  `WinRT.TypeExtensions.GetAbiToProjectionVftblPtr` when the window loads.
- The result is a ~200 MB single file. A framework-dependent publish is
  smaller if that matters more than a single-file drop.

## Build the signed MSIX

```powershell
powershell -ExecutionPolicy Bypass -File .\src\EasyEject.Package\build-msix.ps1
```

Steps performed:

1. `dotnet publish` the UI project into a layout (`obj\layout\x64`),
   framework-dependent .NET, WinAppSDK runtime self-contained in the package,
   bootstrapper auto-initialization disabled.
2. Post-publish fixups (see docs/PACKAGING.md for why): strip the
   `Microsoft.WindowsAppRuntime.Bootstrap*.dll` files, copy the compiled
   `.xbf` files from the build output, and rename `EasyEject.pri` to
   `resources.pri`.
3. Assemble the layout with `makeappx pack` and sign with `signtool` using a
   self-signed code-signing certificate (created/refreshed automatically,
   also imported into `Cert:\CurrentUser\Root`).

Output: `src\EasyEject.Package\bin\EasyEject-x64.msix`.

## Publish a GitHub release asset

Set `GITHUB_TOKEN` to a token with permission to manage releases, then run:

```powershell
dotnet run --project .\scripts\PublishRelease\PublishRelease.csproj -- `
  --repository Bortronx/EasyEject `
  --tag v1.0.0 `
  --release-name "EasyEject 1.0.0" `
  --asset-path .\artifacts\release-assets\EasyEject-1.0.0-win-x64.zip `
  --release-notes "Release notes here"
```

The tool creates the release if the tag does not already exist, removes any
existing asset with the same file name, uploads the new file, and prints the
release URL on success.

## Install the MSIX

```powershell
powershell -ExecutionPolicy Bypass -File .\src\EasyEject.Package\install-msix.ps1
```

- If the shell is not elevated the script relaunches itself as administrator
  (only needed to trust the certificate at machine level; if the cert is
  already trusted in `Cert:\CurrentUser\Root` a plain `Add-AppxPackage`
  works without elevation).
- Uninstall first if the same version is already installed:

```powershell
Get-AppxPackage -Name '*EasyEject*' | Remove-AppxPackage
```

Launch from the Start menu or:

```powershell
explorer.exe shell:AppsFolder\EasyEject_a62za80nbascr!App
```

## Smoke-testing tips

- Logs: `%LOCALAPPDATA%\EasyEject\Logs\easyeject-*.log`
- A healthy launch logs, in order: `EasyEject starting` -> `Host built.` ->
  `MainWindow resolved.` -> `Main window activated.` -> periodic
  `Starting device enumeration` / `Found N disk devices and M volumes.`.
- Kill leftover test instances gracefully (`Stop-Process -Force` skips the
  Serilog flush; use it only for debug runs).
