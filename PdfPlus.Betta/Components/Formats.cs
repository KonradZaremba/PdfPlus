using Betta.Attributes;
using Betta.Interfaces;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using PdfPlus.Betta;
using System.Collections.Generic;
using Sd = System.Drawing;

namespace PdfPlus.Components
{
    // The formatting components set an optional input only when it is wired. Betta hands an unwired optional input over
    // as its default value, so these use a "not set" default instead: NaN for numbers, -1 for the integer inputs that are not
    // enums, (FontStyle)(-1) for the style, null for text and an empty colour.
    [GrasshopperCollection("PDF", "Format")]
    public class Formats : IBettaCollection
    {
        [GrasshopperMethod("Data Set", NickName = "Data Set",
            Description = "Compiles a list of text or numbers into a Data Set with optional formatting options.\r\nFor use with PDF+ Chart and Table components.\r\n - Table: Each DataSet is a column in the Table\r\n - Chart: Each DataSet is a series in the Chart",
            Guid = "926bd11c-44fc-4a6e-8b7c-ebcc7e7e82f9", IconResource = "Pdf-Format-DataSet.png")]
        [return: GrasshopperOutput("DataSet", "Ds", "A PDF Plus Data Object")]
        public DataSet CreateDataSet(
            [GrasshopperParameter("Values", "V", "A list of numeric values")] List<string> values,
            [GrasshopperParameter("Colors", "C", "Optional list of colors corresponding to the values", Optional = true)] List<Sd.Color> colors,
            [GrasshopperParameter("Label", "L", "Optional data label position", Optional = true, DefaultValue = (DataSet.LabelAlignments)0)] DataSet.LabelAlignments label,
            [GrasshopperParameter("Title", "T", "An optional name for the series", Optional = true)] string title)
        {
            if (values == null || values.Count == 0) return null;

            DataSet dataSet = new DataSet(values);

            if (colors != null && colors.Count > 0) dataSet.Colors = colors;
            dataSet.LabelAlignment = label;
            if (title != null) dataSet.Title = title;

            return dataSet;
        }

        [GrasshopperMethod("Quick Data Set", NickName = "Data Set",
            Description = "Compiles a data tree of text or numbers into a list of Data Sets with optional formatting options.\r\nEach list will be compiled into a Data Set and the first branch of the data tree will be converted to the list.\r\n" +
                "Ex. {A}:i[txt] = A[DataSet(i)] || {A;B}:i[txt] = {A}:B[DataSet(i)] || {A;B;C}:i[txt] = {A;B}:B[DataSet(i)]\r\nFor use with PDF+ Chart and Table components.\r\n - Table: Each DataSet is a column in the Table\r\n - Chart: Each DataSet is a series in the Chart",
            Guid = "d2fd6808-d0d9-49a1-8765-617f8c15b067", IconResource = "Pdf-Format-QuickData.png")]
        [return: GrasshopperOutput("DataSet", "Ds", "A PDF Plus Data Object")]
        public List<DataSet> CreateDataSetFast(
            [GrasshopperParameter("Data Tree", "N", "A datatree of text or number values. (Only use numbers for charts)")] GH_Structure<GH_String> dataTree)
        {
            if (dataTree == null || dataTree.PathCount == 0) return null;

            Dictionary<int, List<List<string>>> dataSet = new Dictionary<int, List<List<string>>>();
            List<DataSet> dataSets = new List<DataSet>();

            dataSet.Add(0, new List<List<string>>());
            int i = 0;
            foreach (GH_Path path in dataTree.Paths)
            {
                if (path.Length > 1)
                {
                    if (!dataSet.ContainsKey(path[0])) dataSet.Add(path[0], new List<List<string>>());
                    dataSet[path[0]].Add(dataTree.Branches[i].ToStringList());
                }
                else
                {
                    dataSet[0].Add(dataTree.Branches[i].ToStringList());
                }
                i++;
            }

            // The original picked the list by the component's RunCount (1 on a normal solve); Betta has no run count, so it is always the first.
            foreach (List<string> data in dataSet[0])
            {
                dataSets.Add(new DataSet(data));
            }

            return dataSets;
        }

        [GrasshopperMethod("Set Font", NickName = "Set Font",
            Description = "Get and Set the Font properties of PDF+ Elements including Geometry, Charts, DataSets, Blocks, and Shapes.  ",
            Guid = "12d4cdee-bdf3-4e1d-9bf0-efc493a8e190", IconResource = "Pdf-Format-Font.png")]
        [return: GrasshopperOutput("Element", "El", "A PDF+ Shape, Block, DataSet, or Text Fragment Element", Index = 0)]
        [return: GrasshopperOutput("Family Name", "F", "Font family name", Index = 1)]
        [return: GrasshopperOutput("Size", "S", "Text size", Index = 2)]
        [return: GrasshopperOutput("Color", "C", "Text color", Index = 3)]
        [return: GrasshopperOutput("Styling", "T", "Text styling type", Index = 4)]
        public (object Element, string FamilyName, double Size, Sd.Color Color, int Styling) SetFont(
            [GrasshopperParameter("Element", "El", "A PDF+ Shape, Block, DataSet, Text Fragment Element, or Text (String)")] object element,
            [GrasshopperParameter("Family Name", "F", "Optional Font family name", Optional = true)] string familyName,
            [GrasshopperParameter("Size", "S", "Optional Text size", Optional = true, DefaultValue = double.NaN)] double size,
            [GrasshopperParameter("Color", "C", "Optional Text color", Optional = true)] Sd.Color color,
            [GrasshopperParameter("Styling", "T", "Options Text styling type", Optional = true, DefaultValue = (FontStyle)(-1))] FontStyle styling)
        {
            if (element == null) return default;
            IGH_Goo goo = Wire.Goo(element);

            Font font = new Font();
            Element elem = null;
            if (goo.TryGetElement(ref elem)) font = new Font(elem.Font);

            if (familyName != null) font.Family = familyName;
            if (!double.IsNaN(size)) font.Size = size;
            if (!color.IsEmpty) font.Color = color;
            if ((int)styling >= 0) font.Style = styling;

            Element target = Resolve(goo, elem, true);
            if (target != null) target.Font = font;

            return (target, font.Family, font.Size, font.Color, (int)font.Style);
        }

        [GrasshopperMethod("Set Font Preset", NickName = "Preset Font",
            Description = "Assign a preset Font to a PDF+ Element including geometry, Charts, Data Sets, Blocks, and Shapes.",
            Guid = "61b33e27-7ee2-40ba-a740-c02877df288a", IconResource = "Pdf-Format-FontPresets.png")]
        [return: GrasshopperOutput("Element", "El", "A PDF+ Shape, Block, DataSet, or Text Fragment Element")]
        public object SetFontPreset(
            [GrasshopperParameter("Element", "El", "A PDF+ Shape, Block, DataSet, Text Fragment Element, or Text (String)")] object element,
            [GrasshopperParameter("Font", "F", "Preset fonts", Optional = true, DefaultValue = (Font.Presets)0)] Font.Presets preset)
        {
            if (element == null) return null;
            IGH_Goo goo = Wire.Goo(element);

            Font font = new Font();
            Element elem = null;
            if (goo.TryGetElement(ref elem)) font = new Font(elem.Font);

            switch (preset)
            {
                default:
                    font = Fonts.Normal;
                    break;
                case Font.Presets.Title:
                    font = Fonts.Title;
                    break;
                case Font.Presets.Subtitle:
                    font = Fonts.Subtitle;
                    break;
                case Font.Presets.Heading1:
                    font = Fonts.Heading1;
                    break;
                case Font.Presets.Heading2:
                    font = Fonts.Heading2;
                    break;
                case Font.Presets.Heading3:
                    font = Fonts.Heading3;
                    break;
                case Font.Presets.Heading4:
                    font = Fonts.Heading4;
                    break;
                case Font.Presets.Heading5:
                    font = Fonts.Heading5;
                    break;
                case Font.Presets.Heading6:
                    font = Fonts.Heading6;
                    break;
                case Font.Presets.Quote:
                    font = Fonts.Quote;
                    break;
                case Font.Presets.Caption:
                    font = Fonts.Caption;
                    break;
                case Font.Presets.Footnote:
                    font = Fonts.Footnote;
                    break;
            }

            Element target = Resolve(goo, elem, true);
            if (target != null) target.Font = font;
            return target;
        }

        [GrasshopperMethod("Set Graphics", NickName = "Set Graphics",
            Description = "Get and Set the Graphic properties of PDF+ Elements including geometry, Charts, DataSets, Blocks, and Shapes.",
            Guid = "54c11c7c-45a2-4900-b614-78c22243cd7f", IconResource = "Pdf-Format-Graphic.png")]
        [return: GrasshopperOutput("Element", "El", "A PDF+ Shape, Block, DataSet, or Text Fragment Element", Index = 0)]
        [return: GrasshopperOutput("Fill Color", "F", "The fill color of the shape", Index = 1)]
        [return: GrasshopperOutput("Stroke Color", "S", "The stroke color of the shape", Index = 2)]
        [return: GrasshopperOutput("Stroke Weight", "W", "The stroke weight of the shape", Index = 3)]
        [return: GrasshopperOutput("Pattern", "P", "Dash Pattern as string", Index = 4)]
        public (object Element, Sd.Color FillColor, Sd.Color StrokeColor, double StrokeWeight, string Pattern) SetGraphics(
            [GrasshopperParameter("Element", "El", "A PDF+ Shape, Block, DataSet, Text Fragment Element, or Geometry")] object element,
            [GrasshopperParameter("Fill Color", "F", "Optional fill color for the shape", Optional = true)] Sd.Color fillColor,
            [GrasshopperParameter("Stroke Color", "S", "Optional stroke color for the shape", Optional = true)] Sd.Color strokeColor,
            [GrasshopperParameter("Stroke Weight", "W", "Optional stroke weight for the shape", Optional = true, DefaultValue = double.NaN)] double strokeWeight,
            [GrasshopperParameter("Pattern", "P", "A comma seperated pattern. ex(1.5,2,1.0)", Optional = true)] string pattern)
        {
            if (element == null) return default;
            IGH_Goo goo = Wire.Goo(element);

            Graphic graphic = new Graphic();
            Element elem = null;
            if (goo.TryGetElement(ref elem)) graphic = new Graphic(elem.Graphic);

            if (!fillColor.IsEmpty) graphic.Color = fillColor;
            if (!strokeColor.IsEmpty) graphic.Stroke = strokeColor;
            if (!double.IsNaN(strokeWeight)) graphic.Weight = strokeWeight;
            if (pattern != null) graphic.SetPattern(pattern);

            Element target = Resolve(goo, elem, true);
            if (target != null) target.Graphic = graphic;

            // The original put the pattern list straight into a text output; it is written out as a comma separated string here.
            return (target, graphic.Color, graphic.Stroke, graphic.Weight, string.Join(",", graphic.Pattern));
        }

        [GrasshopperMethod("Set Paragraph Format", NickName = "Set Paragraph",
            Description = "Get and Set the Paragraph properties of PDF+ Text Blocks.  ",
            Guid = "1774216d-cb85-49a8-9888-467f58e84e1e", IconResource = "Pdf-Format-Paragraph.png")]
        [return: GrasshopperOutput("Element", "El", "A PDF+ Shape, Block, DataSet, or Text Fragment Element", Index = 0)]
        [return: GrasshopperOutput("Justification", "J", "Text justification", Index = 1)]
        [return: GrasshopperOutput("Line Spacing", "S", "Optional line spacing value", Index = 2)]
        [return: GrasshopperOutput("Spacing Before", "B", "Optional spacing before the paragraph", Index = 3)]
        [return: GrasshopperOutput("Spacing After", "A", "Optional spacing after the paragraph", Index = 4)]
        [return: GrasshopperOutput("Indentation Left", "L", "Optional indentation to the left of the paragraph", Index = 5)]
        [return: GrasshopperOutput("Indentation Right", "R", "Optional indentation to the right of the paragraph", Index = 6)]
        [return: GrasshopperOutput("Horizontal Borders", "H", "Optional line spacing value", Index = 7)]
        [return: GrasshopperOutput("Vertical Borders", "V", "Optional line spacing value", Index = 8)]
        public (object Element, int Justification, double LineSpacing, double SpacingBefore, double SpacingAfter, double IndentationLeft, double IndentationRight, int HorizontalBorders, int VerticalBorders) SetParagraph(
            [GrasshopperParameter("Element", "El", "A PDF+ Shape, Block, DataSet, Text Fragment Element, or Text (String)")] object element,
            [GrasshopperParameter("Justification", "J", "Text justification", Optional = true, DefaultValue = -1)] int justification,
            [GrasshopperParameter("Line Spacing", "S", "Optional line spacing value", Optional = true, DefaultValue = double.NaN)] double lineSpacing,
            [GrasshopperParameter("Spacing Before", "B", "Optional spacing before the paragraph", Optional = true, DefaultValue = double.NaN)] double spacingBefore,
            [GrasshopperParameter("Spacing After", "A", "Optional spacing after the paragraph", Optional = true, DefaultValue = double.NaN)] double spacingAfter,
            [GrasshopperParameter("Indentation Left", "L", "Optional indentation to the left of the paragraph", Optional = true, DefaultValue = double.NaN)] double indentationLeft,
            [GrasshopperParameter("Indentation Right", "R", "Optional indentation to the right of the paragraph", Optional = true, DefaultValue = double.NaN)] double indentationRight,
            [GrasshopperParameter("Horizontal Border", "H", "Optional paragraph horizontal border setting", Optional = true, DefaultValue = -1)] int horizontalBorder,
            [GrasshopperParameter("Vertical Border", "V", "Optional paragraph vertical border setting", Optional = true, DefaultValue = -1)] int verticalBorder)
        {
            if (element == null) return default;
            IGH_Goo goo = Wire.Goo(element);

            Font font = new Font();
            Element elem = null;
            if (goo.TryGetElement(ref elem)) font = new Font(elem.Font);

            if (justification >= 0) font.Position = (Position)justification;
            if (!double.IsNaN(lineSpacing)) font.LineSpacing = lineSpacing;
            if (!double.IsNaN(spacingBefore)) font.SpaceBefore = spacingBefore;
            if (!double.IsNaN(spacingAfter)) font.SpaceAfter = spacingAfter;
            if (!double.IsNaN(indentationLeft)) font.IndentLeft = indentationLeft;
            if (!double.IsNaN(indentationRight)) font.IndentRight = indentationRight;

            // Data Sets are not handled here: like the original they fall through to the Text Fragment case.
            Element target = Resolve(goo, elem, false);
            int horizontal = 0;
            int vertical = 0;
            if (target != null)
            {
                target.Font = font;
                if (horizontalBorder >= 0) target.HorizontalBorderStyle = GetStyle(horizontalBorder);
                horizontal = (int)target.HorizontalBorderStyle;
                if (verticalBorder >= 0) target.VerticalBorderStyle = GetStyle(verticalBorder);
                vertical = (int)target.VerticalBorderStyle;
            }

            return (target, (int)font.Position, font.LineSpacing, font.SpaceBefore, font.SpaceAfter, font.IndentLeft, font.IndentRight, horizontal, vertical);
        }

        // The Block / Shape / DataSet / Fragment switch the four Set components share, returning the (copied) element to modify.
        // The original dereferenced `elem` without a null check, so a value that is not an Element threw; it is treated as a Fragment here.
        private static Element Resolve(IGH_Goo goo, Element elem, bool includeData)
        {
            switch (elem?.ElementType)
            {
                case Element.ElementTypes.Block:
                    return goo.CastToBlock();
                case Element.ElementTypes.Shape:
                    Shape shape = null;
                    goo.TryGetShape(ref shape);
                    return shape;
                case Element.ElementTypes.Data when includeData:
                    return goo.CastToDataSet();
                default:
                    Fragment fragment = null;
                    return goo.TryGetFragment(ref fragment) ? fragment : null;
            }
        }

        private static Element.BorderStyles GetStyle(int index)
        {
            switch (index)
            {
                default:
                    return Element.BorderStyles.None;
                case 0:
                    return Element.BorderStyles.All;
                case 1:
                    return Element.BorderStyles.Start;
                case 2:
                    return Element.BorderStyles.End;
            }
        }
    }
}
