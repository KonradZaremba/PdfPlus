using Betta.Interfaces;
using Betta.Preview;
using Grasshopper.Kernel.Types;
using PdfPlus.Components;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using PdfSharp.Pdf.IO;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace PdfPlus.Betta.Tests
{
    /// <summary>
    /// The ported component methods called the way Betta calls them (plain values in, plain values out), inside Rhino 8's
    /// core, writing real PDFs that are then read back with PDFsharp.
    /// </summary>
    [Collection(RhinoCollection.Name)]
    public class PdfOutputTests : IDisposable
    {
        private readonly RhinoFixture rhino;
        private readonly string dir;
        private readonly Blocks blocks = new Blocks();
        private readonly Pages pages = new Pages();
        private readonly Documents documents = new Documents();
        private readonly Shapes shapes = new Shapes();
        private readonly Report report = new Report();

        public PdfOutputTests(RhinoFixture rhino)
        {
            this.rhino = rhino;
            dir = Path.Combine(Path.GetTempPath(), "pdfplus-betta-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(dir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        [Fact]
        public void TextBlocksOnACreatedPageSaveAsOneA4Page()
        {
            string path = rhino.Run(() =>
            {
                Block title = blocks.Text("PDF+ on Betta", Font.Presets.Title);
                Block body = blocks.Text("Written by the ported components.", Font.Presets.Normal);
                var (page, boundary) = pages.Create(Plane.WorldXY, Units.Millimeter, 210, 297);
                AssertNear(595.3, boundary.Width);
                AssertNear(841.9, boundary.Height);
                Document doc = blocks.SetBlocks(page, new List<object> { title, body });
                return documents.Save(doc, dir, "text", true, report);
            });

            Assert.Equal(Path.Combine(dir, "text.pdf"), path);
            using PdfDocument pdf = Open(path);
            Assert.Equal(1, pdf.PageCount);
            AssertNear(595.3, pdf.Pages[0].Width.Point);
            AssertNear(841.9, pdf.Pages[0].Height.Point);
            string text = PageText(pdf.Pages[0]);
            AssertShows("PDF+ on Betta", text);
            AssertShows("Written by the ported components.", text);
            Assert.Empty(report.Warnings);
        }

        [Fact]
        public void ADocumentOfThreePagesKeepsEachPageSize()
        {
            string path = rhino.Run(() =>
            {
                Page a4 = pages.CreateMetric(Plane.Unset, SizesA.A4, PageOrientation.Default).Page;
                Page letter = pages.CreateImperial(Plane.Unset, SizesImperial.Letter, PageOrientation.Default).Page;
                Page a3 = pages.CreateMetric(Plane.Unset, SizesA.A3, PageOrientation.Landscape).Page;
                // PDF+ writes no page that has nothing on it, so each gets a line.
                var line = new List<object> { new LineCurve(new Point3d(50, 50, 0), new Point3d(300, 50, 0)) };
                Document doc = documents.Create(null, new List<object>
                {
                    shapes.SetShapes(a4, line), shapes.SetShapes(letter, line), shapes.SetShapes(a3, line),
                });
                return documents.Save(doc, dir, "sizes", true, report);
            });

            using PdfDocument pdf = Open(path);
            Assert.Equal(3, pdf.PageCount);
            // Standard sizes come from PDFsharp's PageSize table, in whole points (A4 595 x 842, not 595.3 x 841.9).
            AssertSize(pdf.Pages[0], 595, 842);
            AssertSize(pdf.Pages[1], 612, 792);
            AssertSize(pdf.Pages[2], 1191, 842);
        }

        [Fact]
        public void MetadataIsWrittenToThePdf()
        {
            string path = rhino.Run(() =>
            {
                Document doc = OneTextPage("metadata");
                var meta = documents.MetaData(doc, "Facade report", "Panel schedule");
                Assert.Equal("Facade report", meta.Title);
                return documents.Save(meta.Document, dir, "meta", true, report);
            });

            using PdfDocument pdf = Open(path);
            Assert.Equal("Facade report", pdf.Info.Title);
            Assert.Equal("Panel schedule", pdf.Info.Subject);
        }

        [Fact]
        public void APasswordLocksThePdf()
        {
            const string password = "test-password";
            string path = rhino.Run(() =>
                documents.Save(documents.Password(OneTextPage("locked"), password), dir, "locked", true, report));

            Assert.ThrowsAny<Exception>(() => Open(path).Dispose());
            using PdfDocument pdf = PdfReader.Open(path, password, PdfDocumentOpenMode.Import);
            Assert.Equal(1, pdf.PageCount);
        }

        [Fact]
        public void RhinoCurvesAndTextShapesDrawOnAPage()
        {
            string path = rhino.Run(() =>
            {
                Page page = pages.Create(Plane.WorldXY, Units.Millimeter, 210, 297).Page;
                Shape label = shapes.TextPoint("Panel A-12", new Plane(new Point3d(60, 700, 0), Vector3d.ZAxis));
                var drawing = new List<object>
                {
                    new LineCurve(new Point3d(60, 100, 0), new Point3d(500, 100, 0)),
                    new ArcCurve(new Circle(new Point3d(300, 400, 0), 120)),
                    label,
                };
                Page drawn = shapes.SetShapes(page, drawing);
                Assert.Equal(3, shapes.GetShapes(drawn, false).Count);

                // Grasshopper draws a Page from its preview box; it has to cover the circle.
                BoundingBox box = ((IBettaPreview)drawn).ClippingBox;
                Assert.True(box.IsValid);
                Assert.True(box.Contains(new Point3d(300, 520, 0)));

                return documents.Save(documents.Create(null, new List<object> { drawn }), dir, "shapes", true, report);
            });

            using PdfDocument pdf = Open(path);
            Assert.Equal(1, pdf.PageCount);
            AssertShows("Panel A-12", PageText(pdf.Pages[0]));
        }

        [Fact]
        public void SaveWritesNothingUntilToggledAndWarnsAboutAMissingFolder()
        {
            rhino.Run(() =>
            {
                Document doc = OneTextPage("not saved");
                Assert.Null(documents.Save(doc, dir, "off", false, report));
                Assert.False(File.Exists(Path.Combine(dir, "off.pdf")));

                Assert.Null(documents.Save(doc, Path.Combine(dir, "missing"), "x", true, report));
                Assert.Single(report.Warnings);
            });
        }

        [Fact]
        public void InputsWorkAsPlainValuesAndAsGrasshopperGoo()
        {
            // Betta hands a method the value inside a wire; a hand-written generic input would hold a GH_ObjectWrapper.
            rhino.Run(() =>
            {
                Page page = pages.Create(Plane.WorldXY, Units.Millimeter, 210, 297).Page;
                Block block = blocks.Text("Same either way", Font.Presets.Normal);

                Document plain = blocks.SetBlocks(page, new List<object> { block });
                Document goo = blocks.SetBlocks(new GH_ObjectWrapper(page), new List<object> { new GH_ObjectWrapper(block) });

                Assert.NotNull(plain);
                Assert.NotNull(goo);
                Assert.Equal(documents.Deconstruct(plain).Count, documents.Deconstruct(goo).Count);
            });
        }

        private Document OneTextPage(string text) =>
            blocks.SetBlocks(pages.Create(Plane.WorldXY, Units.Millimeter, 210, 297).Page,
                new List<object> { blocks.Text(text, Font.Presets.Normal) });

        // Every PDF+ document gets a random owner password, so read-only import is the open mode that needs none.
        private static PdfDocument Open(string path)
        {
            Assert.True(path != null && File.Exists(path), $"No PDF at {path}");
            return PdfReader.Open(path, PdfDocumentOpenMode.Import);
        }

        private static void AssertSize(PdfPage page, double width, double height)
        {
            AssertNear(width, page.Width.Point);
            AssertNear(height, page.Height.Point);
        }

        private static void AssertNear(double expected, double actual) => Assert.InRange(actual, expected - 0.1, expected + 0.1);

        // MigraDoc places words one by one, so spaces are positions rather than characters: compare without them.
        private static void AssertShows(string expected, string pageText)
        {
            static string Squash(string s) => new string(s.Where(c => !char.IsWhiteSpace(c)).ToArray());
            Assert.Contains(Squash(expected), Squash(pageText));
        }

        /// <summary>The strings a page shows (Tj, TJ, ' and " operands), joined.</summary>
        private static string PageText(PdfPage page)
        {
            var text = new StringBuilder();
            Collect(ContentReader.ReadContent(page), text);
            return text.ToString();
        }

        private static void Collect(CObject obj, StringBuilder text)
        {
            switch (obj)
            {
                case CString s: text.Append(s.Value); break;
                case COperator op:
                    if (op.OpCode.Name is "Tj" or "TJ" or "'" or "\"")
                        foreach (CObject operand in op.Operands) Collect(operand, text);
                    break;
                case CSequence seq:
                    foreach (CObject item in seq) Collect(item, text);
                    break;
            }
        }

        private sealed class Report : IBettaReport
        {
            public readonly List<string> Warnings = new List<string>();
            public void Warning(string message) => Warnings.Add(message);
            public void Remark(string message) { }
        }
    }
}
