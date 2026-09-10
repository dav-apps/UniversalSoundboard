# UWP on .NET 10

UniversalSoundboard now targets `net10.0-windows10.0.26100.0` using SDK-style projects and `UseUwp`. UWP XAML and the AppContainer application model are retained. Windows 10 version 2004 (build 19041) remains the minimum supported desktop version.

## Runtime and packaging

The application and Windows Forms hotkey companion ship self-contained .NET 10 runtimes. Trimming and Native AOT are disabled for these executables: the current GraphQL/Newtonsoft.Json, SQLite and Google API integration uses reflection/dynamic facilities. Enabling AOT globally is a separate compatibility task, not a packaging optimization that can safely be switched on without testing.

The pitch-shift effect is a C#/WinRT component compiled with Native AOT. The packaging project publishes it and registers `AudioEffectComponent.Native.dll` for `AudioEffectComponent.PitchShiftAudioEffect`. Its audio buffer access uses generated COM interop instead of the legacy runtime COM cast.

The separate packaging project remains necessary to combine the UWP application, full-trust hotkey companion and native effect. Its application reference uses `UseLowTrustEntryPoint`. Package identity, protocol activation, file associations, share target and the hotkey app service are preserved.

Supported build architectures are x86, x64 and ARM64. ARM32 and the ambiguous solution-wide Any CPU configurations are removed. A .NET Standard library can still use Any CPU internally; the external `../davClassLibrary` project remains a .NET Standard 2.0 dependency.

## Prerequisites and build

- Visual Studio 2026 with UWP tools.
- .NET 10 SDK; `global.json` permits installed stable .NET 10 feature bands.
- Windows SDK 10.0.26100.0 or later with the UWP XAML compiler.
- MSVC C++ build tools for the selected architecture, including ARM64 tools when targeting ARM64.
- The existing ignored `UniversalSoundBoard/Strings/Env.cs` application configuration.

From PowerShell:

```powershell
.\build.ps1 -Configuration Debug -Platform x64
.\build.ps1 -Configuration Release -Platform x64
.\build.ps1 -Configuration Debug -Platform x64 -IncludeTests
dotnet run --project UniversalSoundboard.Tests/Regression/Regression.csproj
```

`build.ps1` initializes the Visual Studio C++ environment and builds an unsigned, single-architecture MSIX for local validation. It does not install the package or publish to the Store. Production signing and Store bundle creation remain separate release steps. Build `UniversalSoundboard.Packaging` as the startup/deployment project in Visual Studio.

## Dependencies

Current `CommunityToolkit.Uwp.*` packages supply animations and visual-tree helpers. The legacy Toolkit notification/loading/splitter controls and RichTextControls are rebuilt in `UniversalSoundboard.Compatibility`; see its README and retained MIT licenses. Tile notifications now use the existing notifications package instead of the incompatible `NotificationsExtensions.Win10` binary. AngleSharp and System.Drawing.Common are explicitly upgraded for the modern runtime.

Win2D is referenced explicitly because its native WinRT registration build assets are not imported through the Toolkit's transitive dependency. The separate packaging project includes its WinMD and architecture-specific native DLL at the package root and enables `AppxHarvestWinmdRegistration`. The application publish output also contains the DLL in its own directory; the root copy is the registered WinRT server. Copying the DLL alone is insufficient: without these registrations, the profile shadow on AccountPage throws `REGDB_E_CLASSNOTREG`. When changing packaging, verify that the final manifest registers `Microsoft.Graphics.Canvas.Geometry.CanvasGeometry` and `Microsoft.Graphics.Canvas.CanvasDevice` against the packaged DLL, and open AccountPage in the installed package.

The old `.rd.xml` files are no longer build inputs. They do not configure .NET 10 trimming or Native AOT. WinRT converter classes are partial so the C#/WinRT generator can supply interop support. Debug telemetry continues to use Sentry's `debug` environment.

Win2D also requires the `Microsoft.VCLibs` UWP SDK reference in the packaging project. Its native DLL imports `MSVCP140_APP.dll` and `VCRUNTIME140_APP.dll`; the separate `UWPDesktop` runtime used by the companion does not supply these. Missing this dependency produces `0x8007007E` when activating CanvasGeometry even when its class is registered.

## Verification scope

Validated locally on x64:

- Debug and Release MSIX builds, including the Native AOT audio component.
- Full Debug solution build, including the migrated test project.
- All 34 packaged UWP tests and all 50 standalone regression checks pass.
- The running application and hotkey companion load .NET 10.0.12; the audio effect loads its native DLL.
- Playback/pause, output-device management, the Plus offer dialog and German resources work in the development package. Debug Sentry events arrive under the test release.

The UWP test host uses an explicit MTA entry point and delegates XAML metadata to the application's generated provider. Generating another XAML application in a project that references the application executable caused the compiler to instantiate a second `Application`, which UWP rejects. Test setup also resets simulated login state between tests.

After building with `-IncludeTests`, run tests in Visual Studio's Test Explorer, or invoke its `vstest.console.exe` with:

```powershell
vstest.console.exe UniversalSoundboard.Tests/bin/x64/Debug/net10.0-windows10.0.26100.0/win-x64/UniversalSoundboard.Tests.build.appxrecipe /Platform:x64
```

The packaging restore still reports `NU1605` through HtmlAgilityPack's legacy UWP dependency graph; the executable itself resolves .NET 10 assets. The test resource index emits `PRI263` for MSTest's neutral resources. These warnings are not suppressed. Existing obsolete API warnings in application code remain separate cleanup work.

The standalone regression checks cover URL parsing, serialized initialization and diagnostics. Hardware changes, payments, account synchronization, importing/exporting real libraries and ARM64/x86 execution require integration testing on the appropriate machines and accounts before release.

Reference: [Microsoft's UWP modernization guide](https://learn.microsoft.com/en-us/windows/uwp/dotnet-native/modernize-uwp-apps-with-dotnet).
