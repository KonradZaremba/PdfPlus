# PDF+ on Betta

An experiment: [PDF+](https://github.com/interopxyz/PdfPlus) by David Mans, with its 48 hand-written Grasshopper components replaced by plain C# methods that [Betta](https://github.com/KonradZaremba/betta) turns into components at load time. The PDF engine (`PdfPlus/Classes`, `PdfPlus/Utilities`) is compiled unchanged. For using PDF+, install the original from [Food4Rhino](https://www.food4rhino.com/en/app/pdf); this port exists to measure what moving an existing plugin onto Betta takes.

Results, method and what changed for users: [the use case write-up in the Betta repo](https://github.com/KonradZaremba/betta/blob/main/usecases/pdfplus-port/README.md).

## Layout

| File | What it is |
|---|---|
| `Components/Documents.cs`, `Pages.cs`, `Blocks.cs`, `Shapes.cs`, `Formats.cs` | The 48 components, one `[GrasshopperMethod]` each, grouped by the original toolbar panels (Doc, Page, Block, Shape, Format) |
| `Wires.cs` | PDF+ objects as typed wires (`IBettaValue`), their preview and bake (`IBettaPreview`, `IBettaBakeable`), `Wire.Goo`, and the code-page setup .NET 7 needs |
| `PreviewDraw.cs` | The drawing and baking code of the original `GH_Pdf__Base`, made static |
| `PdfPlus.Betta.csproj` | Links the engine's source files from `../PdfPlus`; defines `BETTA` for the one engine line that differs |

Each method keeps its original component's GUID and icon, so `.gh` files saved with PDF+ open with this port.

## Build

```bash
dotnet build PdfPlus.Betta -c Release
```

Targets Rhino 8 (`net7.0-windows`). PDFsharp and MigraDoc come from nuget.org (`PDFsharp-MigraDoc-gdi` 1.51.5185-beta, `MigraDoc.Extensions` 1.0.1).

## Install

1. Install Betta 0.8 or later in Rhino 8 (Package Manager, search "Betta").
2. Copy these files from `bin/Release/net7.0-windows/` into `%AppData%\Grasshopper\Libraries\Betta\`, with `PdfPlus.Betta.dll` last (Betta loads a plugin the moment it appears, so its libraries must already be there):
   `HtmlAgilityPack.dll`, `MarkdownSharp.dll`, `MigraDoc.DocumentObjectModel-gdi.dll`, `MigraDoc.Extensions.dll`, `MigraDoc.Extensions.Html.dll`, `MigraDoc.Extensions.Markdown.dll`, `MigraDoc.Rendering-gdi.dll`, `MigraDoc.RtfRendering-gdi.dll`, `PdfSharp-gdi.dll`, `PdfSharp.Charting-gdi.dll`, `PdfPlus.Betta.dll`.

Don't install it next to the original PDF+: both publish the same component IDs.

## Test

```bash
dotnet test PdfPlus.Betta.Tests
```

12 tests; Rhino 8 must be installed (set `RHINO8_SYSTEM_DIR` if it isn't in `C:\Program Files\Rhino 8\System`).

- `PortContractTests` read the original components' source in `PdfPlus/Components` and check that the port publishes the same 48 components: IDs, names, nicknames, tab and panel, inputs and outputs in order, and icons.
- `PdfOutputTests` start Rhino 8's core in the test process (no window, a headless document), call the component methods, save PDFs and read them back with PDFsharp: text, page sizes, metadata, password, Rhino curves and text shapes, the Save toggle, and plain values versus Grasshopper goo as inputs.

Not covered: the Grasshopper canvas (wiring by hand, viewport preview, bake).

## License

MIT, as PDF+. The engine and icons are David Mans' work; the port adds the files in `PdfPlus.Betta` and `PdfPlus.Betta.Tests`.
