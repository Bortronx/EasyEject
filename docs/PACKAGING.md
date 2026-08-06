# WinUI 3 MSIX packaging notes

Hard-won lessons from getting a WinUI 3 / Windows App SDK 1.8 app to launch
from a signed, sideloaded MSIX. Each item below caused a real, confusing
launch-time failure.

## 1. Bootstrapper auto-initialization

WinAppSDK generates a bootstrapper auto-initializer for unpackaged apps. For a
**packaged** app (or an unpackaged app that ships the runtime self-contained)
that auto-initializer must be disabled, otherwise the process tries to bind
`Microsoft.WindowsAppRuntime.Bootstrap.Net` against the machine-installed
framework package and fails.

Symptoms:

- `.NET Runtime` event: `FileNotFoundException: Could not load file or
  assembly 'Microsoft.WindowsAppRuntime.Bootstrap.Net, Version=1.8.0.0...'`
- Windows App Runtime event id 50: `Bootstrapper initialization failed while
  looking for version 1.8`.
- With the bootstrap DLLs present but the runtime not registered: a hang right
  after `EasyEject starting` (no `Host built.` line).

Fix:

```xml
<WindowsAppSdkBootstrapInitialize>false</WindowsAppSdkBootstrapInitialize>
```

in the csproj, and in `build-msix.ps1` delete
`Microsoft.WindowsAppRuntime.Bootstrap*.dll` from the layout after publish.

## 2. Missing .xbf files

The WinUI XAML compiler compiles every `.xaml` into a `.xbf` and copies it to
the normal build output (`App.xbf`, `MainWindow.xbf`, `Views\HomePage.xbf`,
...). `dotnet publish -o <layout>` does **not** carry those `.xbf` files into
the layout. Packaged apps resolve `ms-appx:///...xaml` to those loose `.xbf`
files.

Symptom: `XamlParseException 0x802B000A` ("XAML parsing failed") at
`Xxx.InitializeComponent()` -> `Application.LoadComponent`, with no further
details, plus an `Application Error` event with exception code `0xc000027b`
(stowed exception) in `Microsoft.UI.Xaml.dll`.

Fix: copy `**/*.xbf` from `bin\Release\net9.0-windows10.0.19041.0\win-x64`
into the layout (preserving relative paths) before packing.

## 3. resources.pri vs EasyEject.pri

A packaged app's resource map is resolved from a file named `resources.pri`
in the package root. The raw publish layout keeps the app pri under its own
name (`EasyEject.pri`), so packaged resource lookup finds nothing.

Symptom: after fixing the .xbf files you get
`Cannot locate resource from 'ms-appx:///Microsoft.UI.Xaml/Themes/themeresources.xaml'`
during App.xaml loading (still `0x802B000A`).

Fix: copy `EasyEject.pri` to `resources.pri` in the layout before packing.
(The unpackaged app works fine with either name, which makes this easy to
miss.)

## 4. Framework dependency vs self-contained runtime

If the package bundles the WinAppSDK runtime (self-contained) but the
`AppxManifest.xml` still declares a `PackageDependency` on
`Microsoft.WindowsAppRuntime.1.8`, the app fail-fasts at startup with
`0xc0000602` in `CoreMessagingXP.dll`.

Fix: with a self-contained runtime payload, keep only
`Microsoft.VCLibs.140.00.UWPDesktop` as a package dependency.

## 5. Do not trim

`PublishTrimmed=true` (or `TrimMode=partial`) strips CsWinRT interface
marshaling metadata. The app launches, the window activates, and then the
first XAML binding crashes with:

```
System.NullReferenceException
  at WinRT.TypeExtensions.GetAbiToProjectionVftblPtr(Type helperType)
  ... Set_Microsoft_UI_Xaml_Controls_ItemsControl_ItemsSource ...
```

Fix: `PublishTrimmed=false` (the single-file exe grows to ~200 MB; that is
the trade-off).

## 6. Certificate trust for sideloading

`Add-AppxPackage` of a self-signed package requires the signing certificate
to be trusted. `build-msix.ps1` imports the dev certificate into
`Cert:\CurrentUser\Root` (no elevation needed), which is enough for a
plain, non-elevated `Add-AppxPackage`. The elevated path in
`install-msix.ps1` also trusts it at `Cert:\LocalMachine\Root` for other
user accounts.

## Debugging toolkit

- `Application Error` / `.NET Runtime` / Windows App Runtime event logs give
  module + exception code but little detail.
- The app logs `Application.UnhandledException` (HResult hex, full detail,
  inner-exception chain) to `%LOCALAPPDATA%\EasyEject\Logs\easyeject-*.log` -
  this surfaced every failure above with its real HResult.
- Compare the failing layout against the working unpackaged output
  (`bin\Release\net9.0-windows10.0.19041.0\win-x64`): they must be identical
  apart from the intentionally stripped bootstrap files.
