using Betta.Attributes;
using Betta.Interfaces;
using Rhino.Geometry;

namespace PdfPlus.Components
{
    [GrasshopperCollection("PDF", "Page")]
    public class Pages : IBettaCollection
    {
        [GrasshopperMethod("Create Page Boundary", NickName = "Page Rect", Description = "Create a new PDF Page from a boundary rectangle in Point units",
            Guid = "331173ec-eb7d-4c09-851f-87131c6ec061", IconResource = "Pdf-Page-Add_Rect.png")]
        [return: GrasshopperOutput("Page", "Pg", "A PDF Page")]
        public Page CreateBoundary(
            [GrasshopperParameter("Boundary", "B", "The page rectangular boundary in Points")] Rectangle3d boundary)
        {
            Point3d ptA = boundary.Corner(0);
            Point3d ptB = boundary.Corner(1);
            Point3d ptC = boundary.Corner(3);

            Page page = new Page(Units.Point, ptA.DistanceTo(ptB), ptA.DistanceTo(ptC));
            page.Frame = new Plane(new Plane(ptA, ptB, ptC));
            return page;
        }

        [GrasshopperMethod("Create Page", NickName = "Page", Description = "Create a new PDF Page from a width and height",
            Guid = "b2b9de5c-523b-4bd7-a1b2-41012b3e1d53", IconResource = "Pdf-Page-Add_Custom.png")]
        [return: GrasshopperOutput("Page", "Pg", "A PDF Page", Index = 0)]
        [return: GrasshopperOutput("Boundary", "B", "The page boundary in points", Index = 1)]
        public (Page Page, Rectangle3d Boundary) Create(
            [GrasshopperParameter("Frame", "F", "Optional origin plane of the page", Optional = true)] Plane frame,
            [GrasshopperParameter("Units", "U", "PDF Units", Optional = true, DefaultValue = (Units)0)] Units units,
            [GrasshopperParameter("Width", "W", "The document width in the chosen units", Optional = true, DefaultValue = 210.0)] double width,
            [GrasshopperParameter("Height", "H", "The document height in the chosen units", Optional = true, DefaultValue = 297.0)] double height)
        {
            Page page = new Page(units, width, height);
            if (frame.IsValid) page.Frame = new Plane(frame);
            return (page, page.Boundary);
        }

        [GrasshopperMethod("Create Page Imperial", NickName = "Page Imperial", Description = "Create a new PDF Page from a standard imperial page size",
            Guid = "a41b9784-0cd8-46b9-ab0b-759e633d8d48", IconResource = "Pdf-Page-Add_Imperial.png")]
        [return: GrasshopperOutput("Page", "Pg", "A PDF Page", Index = 0)]
        [return: GrasshopperOutput("Boundary", "B", "The page boundary in points", Index = 1)]
        public (Page Page, Rectangle3d Boundary) CreateImperial(
            [GrasshopperParameter("Frame", "F", "Optional origin plane of the page", Optional = true)] Plane frame,
            [GrasshopperParameter("Type", "T", "A standard Imperial paper size", Optional = true, DefaultValue = (SizesImperial)0)] SizesImperial type,
            [GrasshopperParameter("Orientation", "O", "Set the portrait or landscape orientation of the page", Optional = true, DefaultValue = (PageOrientation)0)] PageOrientation orientation)
        {
            Page page = new Page(type, orientation);
            if (frame.IsValid) page.Frame = new Plane(frame);
            return (page, page.Boundary);
        }

        [GrasshopperMethod("Create Page Metric", NickName = "Page Metric", Description = "Create a new PDF Page from a standard metric page size",
            Guid = "60233390-b0a2-4715-899c-bc900b939a0c", IconResource = "Pdf-Page-Add_Metric.png")]
        [return: GrasshopperOutput("Page", "Pg", "A PDF Page", Index = 0)]
        [return: GrasshopperOutput("Boundary", "B", "The page boundary in points", Index = 1)]
        public (Page Page, Rectangle3d Boundary) CreateMetric(
            [GrasshopperParameter("Frame", "F", "Optional origin plane of the page", Optional = true)] Plane frame,
            [GrasshopperParameter("Type", "T", "A standard Metric A series paper size", Optional = true, DefaultValue = (SizesA)4)] SizesA type,
            [GrasshopperParameter("Orientation", "O", "Set the portrait or landscape orientation of the page", Optional = true, DefaultValue = (PageOrientation)0)] PageOrientation orientation)
        {
            Page page = new Page(type, orientation);
            if (frame.IsValid) page.Frame = new Plane(frame);
            return (page, page.Boundary);
        }

        [GrasshopperMethod("Deconstruct Page", NickName = "DePage", Description = "Deconstruct a PDF Page",
            Guid = "902c28b8-3ec8-47c1-8152-aa2faaa7d152", IconResource = "Pdf-Page-Deconstruct2.png")]
        [return: GrasshopperOutput("Boundary", "B", "The boundary of the page in Points")]
        public Rectangle3d Deconstruct(
            [GrasshopperParameter("Page", "Pg", "A PDF Page")] Page page)
        {
            return new Page(page).Boundary;
        }

        [GrasshopperMethod("Page Boundaries", NickName = "Boundaries", Description = "Get or modify the boundaries of a page",
            Guid = "6ca6613e-f5a3-4f72-8737-fe6d8d116189", IconResource = "Pdf-Page-Margins.png")]
        [return: GrasshopperOutput("Page", "Pg", "A PDF Page", Index = 0)]
        [return: GrasshopperOutput("Art Box", "A", "The art box of the page\r\nYellow preview display color", Index = 1)]
        [return: GrasshopperOutput("Bleed Box", "B", "The bleed box of the page\r\nCyan preview display color", Index = 2)]
        [return: GrasshopperOutput("Crop Box", "C", "The crop box of the page\r\nBlack preview display color", Index = 3)]
        [return: GrasshopperOutput("Trim Box", "T", "The trim box of the page\r\nMagenta preview display color", Index = 4)]
        public (Page Page, Rectangle3d ArtBox, Rectangle3d BleedBox, Rectangle3d CropBox, Rectangle3d TrimBox) Boundaries(
            [GrasshopperParameter("Page", "Pg", "A PDF Page")] Page page,
            [GrasshopperParameter("Art Box", "A", "The art box of the page", Optional = true)] Rectangle3d artBox,
            [GrasshopperParameter("Bleed Box", "B", "The bleed box of the page", Optional = true)] Rectangle3d bleedBox,
            [GrasshopperParameter("Crop Box", "C", "The crop box of the page", Optional = true)] Rectangle3d cropBox,
            [GrasshopperParameter("Trim Box", "T", "The trim box of the page", Optional = true)] Rectangle3d trimBox)
        {
            page = new Page(page);

            // An unwired optional rectangle arrives as default(Rectangle3d), which has no valid plane.
            if (artBox.IsValid) page.ArtBox = artBox;
            if (bleedBox.IsValid) page.BleedBox = bleedBox;
            if (cropBox.IsValid) page.CropBox = cropBox;
            if (trimBox.IsValid) page.TrimBox = trimBox;

            return (page, page.ArtBox, page.BleedBox, page.CropBox, page.TrimBox);
        }
    }
}
