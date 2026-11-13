using Betta.Interfaces;
using Betta.Preview;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace PdfPlus
{
    // PDF+ objects travel on their own typed wires (IBettaValue) instead of exploding into one output per property.
    // Shapes, pages and documents also draw and bake themselves (Betta.Preview), which GH_Pdf__Base did for every
    // component. The one change to the original classes is the word `partial`.

    public partial class Block : IBettaValue { }

    public partial class Fragment : IBettaValue { }

    public partial class DataSet : IBettaValue { }

    public partial class Shape : IBettaValue, IBettaPreview, IBettaBakeable
    {
        BoundingBox IBettaPreview.ClippingBox => BoundingBox;
        void IBettaPreview.DrawWires(IGH_PreviewArgs args) { }
        void IBettaPreview.DrawMeshes(IGH_PreviewArgs args) => Betta.PreviewDraw.Draw(args, Array.Empty<Page>(), new[] { this });
        void IBettaBakeable.Bake(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids) =>
            Betta.PreviewDraw.Bake(doc, att, obj_ids, new List<Page>(), new List<Shape> { this });
    }

    public partial class Page : IBettaValue, IBettaPreview, IBettaBakeable
    {
        BoundingBox IBettaPreview.ClippingBox
        {
            get
            {
                BoundingBox box = Boundary.BoundingBox;
                foreach (Shape shape in Shapes) box.Union(shape.BoundingBox);
                return box;
            }
        }
        void IBettaPreview.DrawWires(IGH_PreviewArgs args) { }
        void IBettaPreview.DrawMeshes(IGH_PreviewArgs args) => Betta.PreviewDraw.Draw(args, new[] { this }, Shapes);
        void IBettaBakeable.Bake(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids) =>
            Betta.PreviewDraw.Bake(doc, att, obj_ids, new List<Page> { this }, Shapes);
    }

    public partial class Document : IBettaValue, IBettaPreview, IBettaBakeable
    {
        // GH_Pdf__Base rendered the blocks to pages once per solve; draw calls come every frame, so keep the result.
        private List<Page> _previewPages;
        private List<Page> PreviewPages => _previewPages ??= RenderBlocksToPages();

        BoundingBox IBettaPreview.ClippingBox
        {
            get
            {
                BoundingBox box = BoundingBox.Empty;
                foreach (Page page in PreviewPages)
                {
                    box.Union(page.Boundary.BoundingBox);
                    foreach (Shape shape in page.Shapes) box.Union(shape.BoundingBox);
                }
                return box;
            }
        }
        void IBettaPreview.DrawWires(IGH_PreviewArgs args) { }
        void IBettaPreview.DrawMeshes(IGH_PreviewArgs args)
        {
            foreach (Page page in PreviewPages) Betta.PreviewDraw.Draw(args, new[] { page }, page.Shapes);
        }
        void IBettaBakeable.Bake(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            foreach (Page page in PreviewPages) Betta.PreviewDraw.Bake(doc, att, obj_ids, new List<Page> { page }, page.Shapes);
        }
    }
}

namespace PdfPlus.Betta
{
    /// <summary>
    /// Betta hands a method the value inside a wire (a Curve, a string, a Shape). PDF+'s converters
    /// (<c>TryGetShape</c>, <c>TryGetFragment</c>, ...) take Grasshopper goo, so this wraps the value back up the way
    /// Grasshopper would have: geometry and primitives as their goo, anything else in a GH_ObjectWrapper, which is
    /// what a hand-written component's generic input received for a PDF+ object.
    /// </summary>
    internal static class Wire
    {
        public static IGH_Goo Goo(object value)
        {
            if (value == null) return null;
            if (value is IGH_Goo goo) return goo;
            return GH_Convert.ToGoo(value) ?? new GH_ObjectWrapper(value);
        }
    }
}
