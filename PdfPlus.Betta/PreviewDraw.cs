using Grasshopper.Kernel;
using Grasshopper.Kernel.Components;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace PdfPlus.Betta
{
    /// <summary>
    /// The viewport preview and bake of GH_Pdf__Base, moved off the component so the PDF objects draw themselves
    /// (Betta.Preview). The bodies are the original's, with three changes: no Hidden/Locked checks (Grasshopper
    /// doesn't ask a hidden component to draw), "Attributes.Selected" is a parameter, and the shapes and pages come in
    /// as arguments instead of fields filled during the solve.
    /// </summary>
    internal static class PreviewDraw
    {
        private static double factor = (72.0 / 96.0) * 0.85;
        private static double bump = (72.0 / 100.0) * 0.995;

        public static void Draw(IGH_PreviewArgs args, IEnumerable<Page> prev_pages, IEnumerable<Shape> prev_shapes, bool selected = false)
        {
            double mTol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            int messageSize = 4;
            string messageFont = "Arial";

            Rhino.Display.DisplayMaterial mat = new Rhino.Display.DisplayMaterial();
            if (selected)
            {
                mat = args.ShadeMaterial_Selected;
            }
            else
            {
                mat = args.ShadeMaterial;
            }

            Color activeColor = mat.Diffuse;

            foreach (Page page in prev_pages)
            {
                args.Display.DrawPatternedPolyline(page.Boundary.ToNurbsCurve().Points.ControlPolygon(), activeColor, 12, 1, false);
                Rhino.Display.Text3d txt = new Rhino.Display.Text3d("All visualizations are for preview purposes only. (Text, Tables, and Lists are estimated, Charts are diagramatic representations, Drawing Graphics are approximated)",page.Boundary.Plane, messageSize);
                args.Display.Draw3dText(txt, activeColor);
            }

            foreach (Shape shape in prev_shapes)
            {
                Plane plane = Plane.WorldXY;

                Color strokeColor = shape.StrokeColor;
                Color fillColor = shape.FillColor;
                Color fontColor = shape.FontColor;
                if (selected)
                {
                    strokeColor = activeColor;
                    fillColor = activeColor;
                    fontColor = activeColor;
                }

                switch (shape.Type)
                {
                    case Shape.ShapeType.PreviewBoundary:
                       args.Display.DrawPatternedPolyline(shape.Polyline, activeColor, 8, 1, false);

                        break;
                    case Shape.ShapeType.PreviewText:
                        args.Display.Draw3dText(shape.ToSingleLineText(),fontColor);

                        break;
                    case Shape.ShapeType.TextObj:

                        Rhino.Display.Text3d text = shape.ToSingleLineText(factor);

                        args.Display.Draw3dText(text, fontColor);
                        args.Display.DrawPoint(shape.Location, Rhino.Display.PointStyle.Circle, 2, activeColor);

                        Point3d[] corners = text.BoundingBox.GetCorners();
                        if (shape.IsUnderlined) args.Display.DrawLine(new Line(corners[0], corners[1]), fontColor);
                        if (shape.IsStrikeout) args.Display.DrawLine(new Line((corners[0] + corners[3]) / 2.0, (corners[1] + corners[2]) / 2.0), fontColor);
                        break;
                    case Shape.ShapeType.TextBox:
                        args.Display.Draw3dText(shape.ToMultiLineTextBlock(bump), fontColor);
                        args.Display.DrawDottedPolyline(shape.PreviewPolyline, activeColor, false);
                        break;

                    case Shape.ShapeType.ChartObj:

                        plane.Origin = shape.PreviewPolyline.BoundingBox.Min;
                        args.Display.DrawDottedPolyline(shape.PreviewPolyline, fillColor,false);
                        List<Curve> crvs = shape.RenderPreviewChart(out List<Color> clrs);
                        if (selected)
                        {
                            for (int i = 0; i < crvs.Count; i++) args.Display.DrawCurve(crvs[i], activeColor, 1);
                        }
                        else
                        {
                            for (int i = 0; i < crvs.Count; i++) args.Display.DrawCurve(crvs[i], clrs[i], 1);
                        }

                        break;
                    case Shape.ShapeType.ImageObj:
                    case Shape.ShapeType.ImageFrame:

                        if (selected)
                        {
                            args.Display.DrawMeshShaded(RectToMesh(shape.PreviewBoundary, shape.Angle), mat);
                        }
                        else
                        {
                            if (shape.ImagePath != "")
                            {
                                Material material = new Material();
                                material.SetBitmapTexture(shape.ImagePath);
                                material.DiffuseColor = Color.White;
                                material.AmbientColor = Color.Black;
                                material.EmissionColor = Color.Black;
                                material.Reflectivity = 0.0;
                                material.ReflectionGlossiness = 0.0;
                                Rhino.Display.DisplayMaterial dmat = new Rhino.Display.DisplayMaterial();

                                args.Display.DrawMeshShaded(RectToMesh(shape.PreviewBoundary,shape.Angle), new Rhino.Display.DisplayMaterial(material));
                            }
                            else
                            {
                                //Rhino.Display.DisplayMaterial m_material = new Rhino.Display.DisplayMaterial();
                                //Rhino.Display.DisplayBitmap favi = new Rhino.Display.DisplayBitmap(shape.Image);
                                
                                //var rt = Rhino.Render.RenderTexture.NewBitmapTexture(viri, Rhino.RhinoDoc.ActiveDoc);
                                //var text = SimulatedTexture(Rhino.Render.RenderTexture.TextureGeneration.Allow).Texture();
                                args.Display.DrawMeshFalseColors(MeshColorByBitmap(shape.PreviewBoundary, shape.Angle, shape.Image, 2));
                            }
                        }
                        break;
                    case Shape.ShapeType.LinkObj:
                        Curve[] linkCurves = new Curve[] { shape.PreviewBoundary.ToNurbsCurve() };
                        Hatch[] linkHatches = Hatch.Create(linkCurves, 0, 0, 1, mTol);
                        foreach (Hatch hatch in linkHatches) args.Display.DrawHatch(hatch, Color.FromArgb(30,activeColor), shape.FillColor);
                        args.Display.DrawDottedPolyline(shape.PreviewPolyline, strokeColor,false);
                        break;
                    case Shape.ShapeType.Arc:
                        args.Display.DrawCurve(shape.Bezier, strokeColor, (int)shape.StrokeWeight);
                        break;
                    case Shape.ShapeType.Bezier:
                        args.Display.DrawCurve(shape.Bezier, strokeColor, (int)shape.StrokeWeight);
                        break;
                    case Shape.ShapeType.Circle:
                        args.Display.DrawCurve(shape.Circle.ToNurbsCurve(), strokeColor, (int)shape.StrokeWeight);
                        break;
                    case Shape.ShapeType.Ellipse:
                        args.Display.DrawCurve(shape.Ellipse.ToNurbsCurve(), strokeColor, (int)shape.StrokeWeight);
                        break;
                    case Shape.ShapeType.Line:
                        args.Display.DrawLine(shape.Line, strokeColor, (int)shape.StrokeWeight);
                        break;
                    case Shape.ShapeType.Polyline:
                        args.Display.DrawPolyline(shape.Polyline, strokeColor, (int)shape.StrokeWeight);
                        break;
                    case Shape.ShapeType.Brep:
                        Curve[] boundaryCurves = shape.Brep.DuplicateNakedEdgeCurves(true, true);
                        Curve[] brepCurves = NurbsCurve.JoinCurves(boundaryCurves);
                        Hatch[] brepHatches = Hatch.Create(brepCurves, 0, 0, 1, 0.1);
                        foreach (Hatch hatch in brepHatches) args.Display.DrawHatch(hatch, fillColor, shape.FillColor);
                        foreach (Curve curve in brepCurves) args.Display.DrawCurve(curve, strokeColor, (int)shape.StrokeWeight);
                        break;
                    case Shape.ShapeType.Mesh:
                        List<Curve> meshCurves = new List<Curve>();
                        Polyline[] meshPlines = shape.Mesh.GetNakedEdges();
                        foreach (Polyline pline in meshPlines) meshCurves.Add(pline.ToNurbsCurve());
                        Hatch[] meshHatches = Hatch.Create(meshCurves, 0, 0, 1, mTol);
                        foreach (Hatch hatch in meshHatches) args.Display.DrawHatch(hatch, fillColor, shape.FillColor);
                        foreach (Curve curve in meshCurves) args.Display.DrawCurve(curve, strokeColor, (int)shape.StrokeWeight);
                        break;
                }
            }

            // Set Display Override
        }

        private static Mesh MeshColorByBitmap(Rectangle3d rectangle,double angle, Bitmap bitmap, int count)
        {
            int xCount = bitmap.Width / count;
            int yCount = bitmap.Height / count;

            int xStep = count;
            int yStep = count;

            if (xCount > 100)
            {
                xCount = (int)rectangle.Width;
                xStep = (int)Math.Floor((double)bitmap.Width / (double)xCount);
            }

            if (yCount > 100)
            {
                yCount = (int)rectangle.Height;
                yStep = (int)Math.Floor((double)bitmap.Height / (double)yCount);
            }

            Mesh mesh = RectToDenseMesh(rectangle, angle, xCount - 1, yCount - 1);
            List<Color> colors = new List<Color>();

            for (int y = 0; y < yCount; y += 1)
            {
                for (int x = 0; x < xCount; x += 1)
                {
                    colors.Add(bitmap.GetPixel(x*xStep, y*yStep));
                }
            }

            mesh.VertexColors.AppendColors(colors.ToArray());

            return mesh;
        }

        private static Line[] RectToLines(Rectangle3d rectangle)
        {
            Line[] output = new Line[3];
            Point3d[] p = rectangle.ToNurbsCurve().Points.ControlPolygon().ToArray();
            output[0] = new Line(p[0], p[1]);
            output[1] = new Line(p[1], p[2]);
            output[2] = new Line(p[2], p[3]);
            output[3] = new Line(p[3], p[0]);

            return output;
        }

        private static Surface RectToSurface(Rectangle3d rectangle)
        {
            Point3d[] p = rectangle.ToNurbsCurve().Points.ControlPolygon().ToArray();
            Surface srf = NurbsSurface.CreateFromCorners(p[0], p[1], p[2], p[3]);

            return srf;
        }

        private static Mesh RectToDenseMesh(Rectangle3d rectangle, double angle, int x, int y)
        {
            Mesh mesh = Mesh.CreateFromPlane(rectangle.Plane, rectangle.X, rectangle.Y, x, y);
            Plane mirrorPlane = Plane.WorldZX;
            mirrorPlane.Origin = rectangle.Center;
            mesh.Transform(Transform.Mirror(mirrorPlane));
            mesh.Transform(Transform.Rotation(angle / 180.0 * Math.PI, rectangle.Corner(0)));
            return mesh;
        }

        private static Mesh RectToMesh(Rectangle3d rectangle, double angle)
        {
            Mesh mesh = Mesh.CreateFromPlane(rectangle.Plane, rectangle.X, rectangle.Y, 1, 1);
            mesh.Transform(Transform.Rotation(angle / 180.0 * Math.PI, rectangle.Corner(0)));
            return mesh;
        }


        public static void Bake(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids, List<Page> prev_pages, List<Shape> prev_shapes)
        {
            if (prev_pages.Count > 0)
            {
                foreach(Page page in prev_pages)
                {
                    GH_CustomPreviewItem item = new GH_CustomPreviewItem();
                    item.Geometry = (IGH_PreviewData)GH_Convert.ToGeometricGoo(page.Boundary);
                    GH_Material shader = new GH_Material(Color.Black);
                    item.Material = shader;
                    item.Shader = shader.Value;
                    item.Colour = shader.Value.Diffuse;
                    Guid guid = item.PushToRhinoDocument(doc, att);
                    if (guid != Guid.Empty)
                    {
                        obj_ids.Add(guid);
                    }
                }
            }
                if (prev_shapes.Count > 0)
            {
                if (att == null) att = doc.CreateDefaultAttributes();

                foreach (Shape shp in prev_shapes)
                {
                    Guid guid = Guid.Empty;
                    if (shp.Geometry != null)
                    {
                        GH_CustomPreviewItem item = new GH_CustomPreviewItem();
                        item.Geometry = (IGH_PreviewData)GH_Convert.ToGeometricGoo(shp.Geometry);
                        GH_Material shader = new GH_Material(shp.Graphic.Stroke);
                        item.Material = shader;
                        item.Shader = shader.Value;
                        item.Colour = shader.Value.Diffuse;
                        if(shp.Is2d)att.ObjectColor = shp.Graphic.Stroke;
                        if (shp.Is2d) att.PlotColor = shp.Graphic.Stroke;
                        if (shp.Is3d) att.ObjectColor = shp.Graphic.Color;
                        if (shp.Is3d) att.PlotColor = shp.Graphic.Color;
                        guid = item.PushToRhinoDocument(doc, att);
                        if (guid != Guid.Empty)
                        {
                            obj_ids.Add(guid);
                        }
                    }
                    switch (shp.Type)
                    {
                        case Shape.ShapeType.TextBox:
                        case Shape.ShapeType.TextObj:
                            if (shp.Type == Shape.ShapeType.TextObj) guid = doc.Objects.AddText(shp.ToSingleLineText(RhinoMath.UnitScale(UnitSystem.PrinterPoints, doc.ModelUnitSystem) * 96 * factor * 0.9));
                            if (shp.Type == Shape.ShapeType.TextBox) guid = doc.Objects.AddText(shp.ToMultiLineTextBlock(RhinoMath.UnitScale(UnitSystem.PrinterPoints, doc.ModelUnitSystem) * 96 * factor * 1.011));

                            if (guid != Guid.Empty)
                            {
                                obj_ids.Add(guid);
                            }
                            break;
                        case Shape.ShapeType.ChartObj:
                            List<Curve> chartCurves = shp.RenderPreviewChart(out List<Color> chartColors);
                            for (int i = 0; i < chartCurves.Count; i++)
                            {

                                att.ObjectColor = chartColors[i];
                                att.PlotColor = chartColors[i];
                                guid = doc.Objects.AddCurve(chartCurves[i], att);

                                if (guid != Guid.Empty)
                                {
                                    obj_ids.Add(guid);
                                }
                            }
                            break;
                        case Shape.ShapeType.ImageFrame:
                        case Shape.ShapeType.ImageObj:
                            guid = doc.Objects.AddCurve(shp.PreviewBoundary.ToNurbsCurve(), att);
                            break;
                    }
                }
            }
        }

    }
}
