# UWP controls rebuilt for modern .NET

This project preserves the controls used by UniversalSoundboard whose original NuGet packages only contain legacy UWP assemblies. It compiles the sources against .NET 10 and the Windows UWP XAML projections. Current Toolkit controls and animations continue to use the official `CommunityToolkit.Uwp.*` packages.

## Sources and licenses

- `InAppNotification`, `Loading`, `GridSplitter`, and their resources: [Windows Community Toolkit v7.1.3](https://github.com/CommunityToolkit/WindowsCommunityToolkit/tree/v7.1.3), MIT; see `LICENSE.Toolkit.md`.
- `RichText`: [XeonKHJ/UWP-RichTextControls](https://github.com/XeonKHJ/UWP-RichTextControls/tree/5bc007be345652e10a40d1e0b169112ba9f889c6), MIT; see `LICENSE.RichTextControls`.

Local adaptations: SDK-style UWP project; modern Toolkit visual-tree extensions; resource dictionary paths and accessibility resource lookup for this assembly; combined default styles; current AngleSharp dependency. The old Toolkit namespaces are retained for the three ported controls to preserve their API and XAML usage.

Keep these sources and license notices together. Do not reintroduce the legacy binary packages alongside this project.
