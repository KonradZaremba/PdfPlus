using Betta.Attributes;
using Betta.Interfaces;
using PdfPlus.Betta;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Sd = System.Drawing;

namespace PdfPlus.Components
{
    [GrasshopperCollection("PDF", "Shape")]
    public class Shapes : IBettaCollection
    {
        [GrasshopperMethod("Basic Chart Shape", NickName = "Cht Shp", Description = "Create a Bar, Column, Line, or Area Chart Shape within a rectangular boundary.",
            Guid = "65bac445-61d4-472f-8855-e899fc5f5477", IconResource = "Pdf-Shape-Chart_Basic.png")]
        [return: GrasshopperOutput("Shape", "Sh", "PDF Shape")]
        public Shape ChartBasic(
            [GrasshopperParameter("DataSet", "Ds", "Chart Data Set to visualize")] List<DataSet> dataSets,
            [GrasshopperParameter("Boundary", "B", "The rectangular boundary of the Shape", Optional = true)] Rectangle3d boundary,
            [GrasshopperParameter("Type", "T", "The chart format to be displayed", Optional = true, DefaultValue = 0)] int type,
            [GrasshopperParameter("X Axis", "X", "Optional X Axis title for the Graph", Optional = true)] string xAxis,
            [GrasshopperParameter("Y Axis", "Y", "Optional Y Axis title for the Graph", Optional = true)] string yAxis,
            [GrasshopperParameter("Grid Lines", "G", "Optional background grid lines", Optional = true, DefaultValue = 0)] int gridLines,
            [GrasshopperParameter("Legend Location", "L", "Optional Legend location", Optional = true, DefaultValue = (Location)0)] Location legendLocation)
        {
            if (dataSets == null || dataSets.Count == 0) return null;

            // An unwired optional rectangle arrives as default(Rectangle3d), which has no valid plane.
            if (!boundary.IsValid) boundary = new Rectangle3d(Plane.WorldXY, 300, 200);

            Shape shape = Shape.CreateChart(dataSets, (Shape.ChartTypes)type, boundary);

            if (xAxis != null) shape.XAxis = xAxis;
            if (yAxis != null) shape.YAxis = yAxis;

            if ((gridLines == 1) | (gridLines == 3)) shape.HorizontalBorderStyle = Element.BorderStyles.All;
            if (gridLines > 1) shape.VerticalBorderStyle = Element.BorderStyles.All;

            shape.Alignment = legendLocation;
            return shape;
        }

        [GrasshopperMethod("Pie Chart Shape", NickName = "Pie Shp", Description = "Create a Pie Chart Shape within a rectangular boundary",
            Guid = "8b9306b4-9ed2-4b81-bd7b-3b87c413e239", IconResource = "Pdf-Shape-Chart_Pie.png")]
        [return: GrasshopperOutput("Shape", "Sh", "PDF Shape")]
        public Shape ChartPie(
            [GrasshopperParameter("DataSet", "Ds", "A single Chart Data Set to visualize")] DataSet dataSet,
            [GrasshopperParameter("Boundary", "B", "The rectangular boundary of the Shape", Optional = true)] Rectangle3d boundary,
            [GrasshopperParameter("Legend Location", "L", "Optional Legend location", Optional = true, DefaultValue = (Location)0)] Location legendLocation)
        {
            if (dataSet == null) return null;

            if (!boundary.IsValid) boundary = new Rectangle3d(Plane.WorldXY, 300, 200);

            Shape shape = Shape.CreateChart(dataSet, Shape.ChartTypes.Pie, boundary);
            shape.Alignment = legendLocation;
            return shape;
        }

        [GrasshopperMethod("Comment Shape", NickName = "Comment Shp", Description = "Create a Comment Shape at a location",
            Guid = "6f487c10-d602-47a0-b4f9-33e3c9dcb82f", IconResource = "Pdf-Shape-Comment.png")]
        [return: GrasshopperOutput("Shape", "Sh", "PDF Shape")]
        public Shape Comment(
            [GrasshopperParameter("Content", "C", "The content of the comment")] string content,
            [GrasshopperParameter("Location", "P", "The location point of the comment")] Point3d location,
            [GrasshopperParameter("Title", "T", "The title of the comment", Optional = true)] string title,
            [GrasshopperParameter("Subject", "S", "The subject of the comment", Optional = true)] string subject,
            [GrasshopperParameter("Icon", "I", "The comment icon", Optional = true, DefaultValue = 0)] int icon)
        {
            if (content == null) return null;
            return Shape.CreateComment(title ?? "Unnamed", subject ?? "Unspecified", content, location, (Shape.CommentIcons)icon);
        }

        [GrasshopperMethod("Image Box Shape", NickName = "Img Box Shp", Description = "Create an Image Shape within a rectangular boundary",
            Guid = "e191f171-6313-4342-959c-db8ab28cdd0c", IconResource = "Pdf-Shape-Image_Rect.png")]
        [return: GrasshopperOutput("Shape", "Sh", "PDF Shape")]
        public Shape ImageBox(
            [GrasshopperParameter("Image", "I", "The System.Drawing.Bitmap or Image Filepath to display")] object image,
            [GrasshopperParameter("Boundary", "B", "The rectangular boundary of the Shape")] Rectangle3d boundary)
        {
            Sd.Bitmap bitmap = null;
            string path = "";
            if (image == null) return null;
            if (!Wire.Goo(image).TryGetBitmap(ref bitmap, ref path)) return null;

            return Shape.CreateImage(bitmap, boundary, path);
        }

        [GrasshopperMethod("Image Shape", NickName = "Img Shp", Description = "Create an Image Shape at a point location",
            Guid = "d640f163-bd2c-449b-855f-014dbdef46ad", IconResource = "Pdf-Shape-Image_Point.png")]
        [return: GrasshopperOutput("Shape", "Sh", "PDF Shape", Index = 0)]
        [return: GrasshopperOutput("Boundary", "B", "The image boundary in points", Index = 1)]
        public (Shape Shape, Rectangle3d Boundary) ImagePoint(
            [GrasshopperParameter("Image", "I", "The System.Drawing.Bitmap or Image Filepath to display")] object image,
            [GrasshopperParameter("Frame", "F", "The placement plane for the image. The Origin of the plane and XL Plane rotation angle will apply to the image")] Plane frame,
            [GrasshopperParameter("Scale", "S", "A unitized scale factor (0-1) for the image", Optional = true, DefaultValue = 1.0)] double scale)
        {
            Sd.Bitmap bitmap = null;
            string path = "";
            if (image == null) return default;
            if (!Wire.Goo(image).TryGetBitmap(ref bitmap, ref path)) return default;

            Plane p = frame;

            double w = bitmap.Width * (72.0 / 96.0) * scale;
            double h = bitmap.Height * (72.0 / 96.0) * scale;

            Rectangle3d rect = new Rectangle3d(Plane.WorldXY, p.Origin, new Point3d(p.OriginX + w, p.OriginY + h, 0));

            Shape shape = Shape.CreateImage(bitmap, rect, path);
            shape.Angle = Vector3d.VectorAngle(Vector3d.XAxis, p.XAxis, Plane.WorldXY) / Math.PI * 180.0;

            Rectangle3d bnd = new Rectangle3d(Plane.WorldXY, p.Origin, new Point3d(p.OriginX + w, p.OriginY + h, 0));
            bnd.Transform(Transform.Rotation(shape.Angle / 180.0 * Math.PI, p.Origin));

            return (shape, bnd);
        }

        [GrasshopperMethod("Link Shape", NickName = "Lnk Shp", Description = "Create a link to a document, hyperlink, or page within a rectangular boundary",
            Guid = "413ae7fd-0b46-4c15-b17c-dffedbe7f6c9", IconResource = "Pdf-Shape-Link.png")]
        [return: GrasshopperOutput("Shape", "Sh", "PDF Shape")]
        public Shape Link(
            [GrasshopperParameter("Target", "T", "The target of the link", Optional = true, DefaultValue = (Shape.LinkTypes)0)] Shape.LinkTypes target,
            [GrasshopperParameter("Link", "L", "The hyperlink to link to", Optional = true, DefaultValue = "")] string link,
            [GrasshopperParameter("Boundary", "B", "The rectangular boundary of the Shape")] Rectangle3d boundary)
        {
            return Shape.CreateLink(link ?? "", boundary, target);
        }

        [GrasshopperMethod("Text Box Shape", NickName = "Txt Box Shp", Description = "Create a Text Shape within a rectangular boundary",
            Guid = "7ecff30f-8500-4d86-96a1-e4effdcc81c2", IconResource = "Pdf-Shape-Text_Rect.png")]
        [return: GrasshopperOutput("Shape", "Sh", "PDF Shape")]
        public Shape TextBox(
            [GrasshopperParameter("Text Fragment", "Tx", "PDF Text Fragment")] object fragment,
            [GrasshopperParameter("Boundary", "B", "The rectangular boundary of the Shape")] Rectangle3d boundary)
        {
            Fragment frag = null;
            if (fragment == null || !Wire.Goo(fragment).TryGetFragment(ref frag)) return null;

            return Shape.CreateText(frag, boundary, new Font());
        }

        [GrasshopperMethod("Text Line Shape", NickName = "Txt Shp", Description = "Create a Text Shape at a point location",
            Guid = "81f60353-b223-4c36-b46e-588733aadcac", IconResource = "Pdf-Shape-Text_Point.png")]
        [return: GrasshopperOutput("Shape", "Sh", "PDF Shape")]
        public Shape TextPoint(
            [GrasshopperParameter("Text Fragment", "Tx", "PDF Text Fragment")] object fragment,
            [GrasshopperParameter("Frame", "F", "The placement plane for the text. The Origin of the plane and XL Plane rotation angle will apply to the text")] Plane frame)
        {
            Fragment frag = null;
            if (fragment == null || !Wire.Goo(fragment).TryGetFragment(ref frag)) return null;

            Shape shape = Shape.CreateText(frag, frame.Origin, new Font());
            shape.Angle = Vector3d.VectorAngle(Vector3d.XAxis, frame.XAxis, Plane.WorldXY) / Math.PI * 180.0;
            return shape;
        }

        [GrasshopperMethod("Get Shape Contents", NickName = "Contents", Description = "Get geometric, text, and other Shape contents",
            Guid = "8632b176-4bfa-4df5-ac1e-4c6796c2bd17", IconResource = "Pdf-Shape-Contents.png")]
        [return: GrasshopperOutput("Boundary", "B", "Shape Boundary", Index = 0)]
        [return: GrasshopperOutput("Location", "L", "Shape Location Point", Index = 1)]
        [return: GrasshopperOutput("Text", "T", "Text contents when applicable", Index = 2)]
        [return: GrasshopperOutput("Geometry", "G", "Geometry contents when applicable", Index = 3)]
        public (Rectangle3d Boundary, Point3d Location, string Text, GeometryBase Geometry) GetContents(
            [GrasshopperParameter("Shape", "Sh", "PDF Shape")] object shape)
        {
            Shape s = null;
            if (shape == null || !Wire.Goo(shape).TryGetShape(ref s)) return default;

            // The original left an output unset when the Shape had nothing for it; here those come back as default / null.
            return (
                s.Boundary.IsValid ? s.Boundary : default,
                s.Location.IsValid ? s.Location : default,
                s.Text != "" ? s.Text : null,
                s.Geometry);
        }

        [GrasshopperMethod("Get Shapes", NickName = "Get Shp", Description = "Get the Shapes from Pages or Documents.",
            Guid = "c8ef1e93-ef08-47aa-9fcf-3d26b57c716f", IconResource = "Pdf-Shape-Deconstruct.png")]
        [return: GrasshopperOutput("Shape", "Sh", "PDF Shape")]
        public List<Shape> GetShapes(
            [GrasshopperParameter("Page", "Pg", "A PDF Page")] object page,
            [GrasshopperParameter("Preview Blocks", "B", "If true, preview Shapes of Block elements will be rendered", Optional = true, DefaultValue = false)] bool previewBlocks)
        {
            Document document = new Document();
            if (page == null || !Wire.Goo(page).TryGetDocument(ref document)) return null;

            return document.Shapes(previewBlocks);
        }

        [GrasshopperMethod("Set Shapes", NickName = "Set Shp", Description = "Place Text, Image, Geometric, or other Shapes into a PDF Page.",
            Guid = "7e649fc6-716b-4079-90eb-9fd203298a38", IconResource = "Pdf-Shape-Add.png")]
        [return: GrasshopperOutput("Page", "Pg", "A PDF Page")]
        public Page SetShapes(
            [GrasshopperParameter("Page", "Pg", "A PDF Page", Optional = true)] object page,
            [GrasshopperParameter("Shape", "Sh", "PDF Shape", Optional = true)] List<object> shapes)
        {
            Page p = new Page();
            if (page != null) Wire.Goo(page).TryGetPage(ref p);

            if (shapes == null || shapes.Count == 0) return null;
            foreach (object s in shapes) p.AddShape(Wire.Goo(s));

            return p;
        }
    }
}
