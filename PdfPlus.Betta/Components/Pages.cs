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
    }
}
