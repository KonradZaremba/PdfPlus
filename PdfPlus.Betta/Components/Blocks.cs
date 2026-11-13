using Betta.Attributes;
using Betta.Interfaces;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using PdfPlus.Betta;
using System.Collections.Generic;
using Sd = System.Drawing;

namespace PdfPlus.Components
{
    [GrasshopperCollection("PDF", "Block")]
    public class Blocks : IBettaCollection
    {
        [GrasshopperMethod("Text Block", NickName = "Txt Blk", Description = "Create a text Block",
            Guid = "4e9ff875-26bb-4365-a839-1fbccccae6bd", IconResource = "Pdf-Block-Text.png")]
        [return: GrasshopperOutput("Block", "Bk", "PDF Document Block")]
        public Block Text(
            [GrasshopperParameter("Text Fragment", "Tx", "PDF Text Fragment")] object fragment,
            [GrasshopperParameter("Formatting", "F", "Optional predefined text formatting", Optional = true, DefaultValue = (Font.Presets)0)] Font.Presets formatting)
        {
            Fragment frag = null;
            if (!Wire.Goo(fragment).TryGetFragment(ref frag)) return null;
            return Block.CreateText(frag, formatting);
        }

        [GrasshopperMethod("Break Block", NickName = "Brk Blk", Description = "Create a page break or line break Block",
            Guid = "c8f9c9fa-6c24-4934-88af-20b0aeb788f9", IconResource = "Pdf-Block-Break.png")]
        [return: GrasshopperOutput("Block", "Bk", "PDF Document Block")]
        public Block BreakBlock(
            [GrasshopperParameter("Type", "T", "Switch between Line and Page breaks", Optional = true, DefaultValue = 0)] int type,
            [GrasshopperParameter("Count", "C", "The number of repetitions of the break", Optional = true, DefaultValue = 1)] int count)
        {
            if (count < 1) count = 1;

            Block block = Block.CreatePageBreak(count);
            switch (type)
            {
                case 0:
                    block = Block.CreateLineBreak(count);
                    break;
            }
            return block;
        }

        [GrasshopperMethod("Basic Chart Block", NickName = "Cht Blk", Description = "Create a Bar, Column, Line, or Area Chart Block.",
            Guid = "59db45ad-3851-4d74-a6fa-b2007b55cf50", IconResource = "Pdf-Block-Chart_Basic.png")]
        [return: GrasshopperOutput("Block", "Bk", "PDF Document Block")]
        public Block ChartBasic(
            [GrasshopperParameter("DataSet", "Ds", "Chart Data to visualize")] List<DataSet> dataSets,
            [GrasshopperParameter("Type", "T", "The chart format to be displayed", Optional = true, DefaultValue = 0)] int type,
            [GrasshopperParameter("X Axis", "X", "Optional X Axis title for the Graph", Optional = true)] string xAxis,
            [GrasshopperParameter("Y Axis", "Y", "Optional Y Axis title for the Graph", Optional = true)] string yAxis,
            [GrasshopperParameter("Grid Lines", "G", "Optional background grid lines", Optional = true, DefaultValue = 0)] int gridLines,
            [GrasshopperParameter("Legend Location", "L", "Optional Legend location", Optional = true, DefaultValue = (Location)0)] Location legendLocation)
        {
            if (dataSets == null || dataSets.Count == 0) return null;

            Block block = Block.CreateChart(dataSets, (ObjectAssembly.ChartTypes)type);

            if (xAxis != null) block.XAxis = xAxis;
            if (yAxis != null) block.YAxis = yAxis;

            if ((gridLines == 1) | (gridLines == 3)) block.HorizontalBorderStyle = Element.BorderStyles.All;
            if (gridLines > 1) block.VerticalBorderStyle = Element.BorderStyles.All;

            block.Alignment = legendLocation;
            return block;
        }

        [GrasshopperMethod("Pie Chart Block", NickName = "Pie Blk", Description = "Create a Pie Chart Block.",
            Guid = "f959474e-0a17-44d8-9ae3-dd921e5a94db", IconResource = "Pdf-Block-Chart_Pie.png")]
        [return: GrasshopperOutput("Block", "Bk", "PDF Document Block")]
        public Block ChartPie(
            [GrasshopperParameter("DataSet", "Ds", "A single Chart Data Set to visualize")] DataSet dataSet,
            [GrasshopperParameter("Legend Location", "L", "Optional Legend location", Optional = true, DefaultValue = (Location)0)] Location legendLocation)
        {
            if (dataSet == null) return null;

            Block block = Block.CreateChart(dataSet, ObjectAssembly.ChartTypes.Pie);
            block.Alignment = legendLocation;
            return block;
        }

        [GrasshopperMethod("Dock Block", NickName = "Dock Blk",
            Description = "Place a list of Blocks aligned horizontally on the page.\r\nCompatible with Text, List, Chart, Drawing, Table, and Image Blocks. Vertical Groups and nested Docks and Groups are supported" +
                "\r\n(NOTE 1: Vertical Groups do not currently support drawings and adding a Group with Drawing to a Dock or a Dock with a Drawing to a Group will invalidate the drawing.)\r\n" +
                "\r\n(NOTE 2: Rhino Previews for Docks and their content is not yet fully supported. The overall boundary will be returned.)",
            Guid = "1e1aa4a1-326a-4c1f-b524-64930f49a4af", IconResource = "Pdf-Block-Horizontal.png")]
        [return: GrasshopperOutput("Block", "Bk", "PDF Document Block")]
        public Block Dock(
            [GrasshopperParameter("Block", "Bk", "PDF Document Block")] List<object> blocks)
        {
            List<Block> list = GetBlockList(blocks);
            return list.Count > 0 ? Block.CreateDock(list) : null;
        }

        [GrasshopperMethod("Drawing Block", NickName = "Drw Blk", Description = "Create a drawing Block from a list of geometry and text Shapes",
            Guid = "a41fef5d-8f53-4866-a9ad-5dbf4db5cd64", IconResource = "Pdf-Block-Draw.png")]
        [return: GrasshopperOutput("Block", "Bk", "PDF Document Block")]
        public Block Drawing(
            [GrasshopperParameter("Shape", "Sh", "PDF Shape")] List<object> shapes,
            [GrasshopperParameter("Fitting", "F", "Page Fitting\r\n(-1 = Autofit Page\r\n0 = Autofit Content\r\n0 < Fixed Size", Optional = true, DefaultValue = -1)] int fitting)
        {
            if (shapes == null || shapes.Count == 0) return null;

            List<Shape> list = new List<Shape>();
            foreach (object o in shapes)
            {
                Shape shape = null;
                if (o != null && Wire.Goo(o).TryGetShape(ref shape)) list.Add(shape);
            }

            Block block = Block.CreateDrawing(list);
            block.Width = fitting;
            return block;
        }

        [GrasshopperMethod("Join Text Fragments", NickName = "Join Frag",
            Description = "Join Text or PDF+ Text Fragments into a larger fragment while maintainging each fragment's formatting.",
            Guid = "b981432c-3b90-41c3-8895-a994b0c5af63", IconResource = "Pdf-Format-JoinFragment.png")]
        [return: GrasshopperOutput("Text Fragment", "Tx", "PDF Text Fragment")]
        public Fragment JoinFragments(
            [GrasshopperParameter("Text Fragment", "Tx", "PDF Text Fragments or Text")] List<object> fragments)
        {
            if (fragments == null || fragments.Count == 0) return null;

            Fragment output = new Fragment();
            foreach (object o in fragments)
            {
                Fragment fragment = null;
                if (o != null && Wire.Goo(o).TryGetFragment(ref fragment)) output.AddFragments(fragment);
            }

            if (output.Texts.Count < 1) return null;
            return output;
        }

        [GrasshopperMethod("Group Block", NickName = "Group Blk",
            Description = "Group a list of Blocks together vertically.\r\nCompatible with Text, List, Chart, Table, and Image Blocks. Vertical Groups and nested Docks and Groups are supported" +
                "\r\n(NOTE 1: Vertical Groups do not currently support drawings and adding a Group with Drawing to a Dock or a Dock with a Drawing to a Group will invalidate the drawing.)\r\n" +
                "\r\n(NOTE 2: Rhino Previews for Docks and their content is not yet fully supported. The overall boundary will be returned.)",
            Guid = "0eeb6ed0-a2fd-46cd-afd5-a92a78026099", IconResource = "Pdf-Block-Vertical.png")]
        [return: GrasshopperOutput("Block", "Bk", "PDF Document Block")]
        public Block Group(
            [GrasshopperParameter("Block", "Bk", "PDF Document Block")] List<object> blocks)
        {
            List<Block> list = GetBlockList(blocks);
            return list.Count > 0 ? Block.CreateGroup(list) : null;
        }

        [GrasshopperMethod("HTML Block", NickName = "HTML Blk",
            Description = "Create a HTML Block\r\n<h1>Heading 1</h1>\r\n<h2>Heading 2</h2>\r\n<h3>Heading 3</h3>\r\n<h4>Heading 4</h4>\r\n<h5>Heading 5</h5>\r\n<h6>Heading 6</h6>\r\n" +
                "<hr /> 'Horizontal Rule'\r\n<strong>Bold</strong>\r\n<i>Italic</i>\r\n<u>Underline</u>\r\n<strong><i>Bold & Italic</i></strong>\r\n" +
                "<ol><li>Ordered List Item 1</li></ol>\r\n<ul><li>Unordered List Item A</li></ul>\r\n<a href='http://www.duckduckgo.com'>Hyperlink</a>",
            Guid = "8e59f222-ccc5-46c0-a0f1-e0c9026bf65b", IconResource = "Pdf-Block-Html.png")]
        [return: GrasshopperOutput("Block", "Bk", "PDF Document Block")]
        public Block Html(
            [GrasshopperParameter("HTML", "H", "HTML Text", Optional = true)] string html)
        {
            if (html == null) return null;
            return Block.CreateHtml(html);
        }

        [GrasshopperMethod("Image Block", NickName = "Img Blk", Description = "Create an image Block",
            Guid = "1bccdc38-d2d7-4076-b827-da6cce74b558", IconResource = "Pdf-Block-Image.png")]
        [return: GrasshopperOutput("Block", "Bk", "PDF Document Block")]
        public Block ImageBlock(
            [GrasshopperParameter("Image", "I", "The System.Drawing.Bitmap or Image Filepath to display")] object image)
        {
            Sd.Bitmap bitmap = null;
            string path = "";
            if (image == null) return null;
            if (!Wire.Goo(image).TryGetBitmap(ref bitmap, ref path)) return null;

            return path != "" ? Block.CreateImage(path) : Block.CreateImage(bitmap);
        }

        [GrasshopperMethod("List Block", NickName = "Lst Blk", Description = "Create an unordered (bulleted) list or numbered list Block",
            Guid = "0e512243-8cec-406b-bb6b-4a6f16e2456d", IconResource = "Pdf-Block-List.png")]
        [return: GrasshopperOutput("Block", "Bk", "PDF Document Block")]
        public Block ListBlock(
            [GrasshopperParameter("Text Fragment", "Tx", "PDF Text Fragment")] List<object> fragments,
            [GrasshopperParameter("Bullet Type", "B", "The list bullet type", Optional = true, DefaultValue = (Block.ListTypes)0)] Block.ListTypes bulletType)
        {
            if (fragments == null || fragments.Count == 0) return null;

            List<Fragment> outputs = new List<Fragment>();
            foreach (object o in fragments)
            {
                Fragment fragment = null;
                if (o != null && Wire.Goo(o).TryGetFragment(ref fragment)) outputs.Add(fragment);
            }

            return Block.CreateList(outputs, bulletType);
        }

        [GrasshopperMethod("Markdown Block", NickName = "Mkd Blk",
            Description = "Create a Markdown Block\r\n# Heading 1\r\n## Heading 2\r\n### Heading 3\r\n#### Heading 4\r\n##### Heading 5\r\n###### Heading 6\r\n" +
                "*** 'Horizontal Rule'\r\n--- 'Horizontal Rule'\r\n___ 'Horizontal Rule'\r\n**Bold**\r\n*Italic*\r\n***Bold & Italic***\r\n___Bold & Italic___\r\n" +
                "__*Bold & Italic*__\r\n**_Bold & Italic_ **\r\n1.Numbered List Item\r\n- Unordered List Item\r\n* Unordered List Item\r\n+ Unordered List Item\r\n" +
                "[Hyperlink](https://duckduckgo.com)\r\n\\ \\` \\* \\_ \\{ } \\[] \\<> \\() \\# \\+ \\- \\. \\! \\| ",
            Guid = "af7121bf-14ab-4dce-9f29-24882f729539", IconResource = "Pdf-Block-MarkDown.png")]
        [return: GrasshopperOutput("Block", "Bk", "PDF Document Block")]
        public Block Markdown(
            [GrasshopperParameter("Markdown", "M", "Markdown Text", Optional = true)] string markdown)
        {
            if (markdown == null) return null;
            return Block.CreateMarkdown(markdown);
        }

        [GrasshopperMethod("Resize Block", NickName = "Size Blk", Description = "Resize Blocks that support fixed boundaries",
            Guid = "c03a1734-83b7-4ec6-b437-5c37853319ea", IconResource = "Pdf-Block-Size.png")]
        [return: GrasshopperOutput("Block", "Bk", "PDF Document Block")]
        public Block Resize(
            [GrasshopperParameter("Block", "Bk", "PDF Document Block")] object block,
            [GrasshopperParameter("Width", "W", "Optional image width override", Optional = true)] double width,
            [GrasshopperParameter("Height", "H", "Optional image height override", Optional = true)] double height,
            [GrasshopperParameter("Justification", "J", "Optional image justification on page", Optional = true)] Justification justification)
        {
            Block b = null;
            if (block == null || !Wire.Goo(block).TryGetBlock(ref b)) return null;

            // The original only applied the inputs that were wired. Unwired optional inputs arrive as 0 / Justification.None here,
            // so those values mean "leave as is".
            if (width != 0) b.Width = width;
            if (height != 0) b.Height = height;
            if (justification != Justification.None) b.Justification = justification;

            return b;
        }

        [GrasshopperMethod("Table Block", NickName = "Tbl Blk", Description = "Create a table Block list of DataSet objects",
            Guid = "4614f54a-9978-4851-b2a1-ba52a911a29a", IconResource = "Pdf-Block-Table2.png")]
        [return: GrasshopperOutput("Block", "Bk", "PDF Document Block")]
        public Block Table(
            [GrasshopperParameter("DataSet", "Ds", "A list of DataSet objects")] List<DataSet> dataSets,
            [GrasshopperParameter("Border Style", "B", "Table border style", Optional = true, DefaultValue = 0)] int borderStyle,
            [GrasshopperParameter("Heading ", "H", "Table Heading formatting", Optional = true, DefaultValue = 0)] int heading,
            [GrasshopperParameter("Column Fitting", "F", "Table column width \r\n(-1 = Autofit Page\r\n0 = Autofit Content\r\n0 < Fixed Size", Optional = true, DefaultValue = -1)] int columnFitting,
            [GrasshopperParameter("Alternating Color", "A", "Table alternating row color", Optional = true)] Sd.Color alternatingColor)
        {
            if (dataSets == null || dataSets.Count == 0) return null;

            Block block = Block.CreateTable(dataSets);
            ApplyTableOptions(block, borderStyle, heading, columnFitting, alternatingColor);
            return block;
        }

        [GrasshopperMethod("Fast Table Block", NickName = "Fast Tbl Blk",
            Description = "Create a table block from a datatree of text as an input\r\nEach list will be compiled into rows in a single column and the first branch of the data tree will set the column.\r\n" +
                "Ex. {A}:i[txt] = A[DataSet(i)] || {A;B}:i[txt] = {A}:B[DataSet(i)] || {A;B;C}:i[txt] = {A;B}:B[DataSet(i)]",
            Guid = "621a662a-fefb-45ab-997d-efc8d4259ebc", IconResource = "Pdf-Block-Table.png")]
        [return: GrasshopperOutput("Block", "Bk", "PDF Document Block")]
        public Block TableFast(
            [GrasshopperParameter("Text Tree", "T", "A datatree of text values")] GH_Structure<GH_String> textTree,
            [GrasshopperParameter("Border Style", "B", "Table border style", Optional = true, DefaultValue = 0)] int borderStyle,
            [GrasshopperParameter("Heading ", "H", "Table Heading formatting", Optional = true, DefaultValue = 0)] int heading,
            [GrasshopperParameter("Column Fitting", "F", "Table column width \r\n(-1 = Autofit Page\r\n0 = Autofit Content\r\n0 < Fixed Size", Optional = true, DefaultValue = -1)] int columnFitting,
            [GrasshopperParameter("Alternating Color", "A", "Table alternating row color", Optional = true)] Sd.Color alternatingColor)
        {
            if (textTree == null || textTree.PathCount == 0) return null;

            Dictionary<int, List<List<string>>> dataSets = new Dictionary<int, List<List<string>>>();
            List<List<string>> dataSet = new List<List<string>>();

            dataSets.Add(0, new List<List<string>>());
            int i = 0;
            foreach (GH_Path path in textTree.Paths)
            {
                if (path.Length > 1)
                {
                    if (!dataSets.ContainsKey(path[0])) dataSets.Add(path[0], new List<List<string>>());
                    dataSets[path[0]].Add(textTree.Branches[i].ToStringList());
                }
                else
                {
                    dataSets[0].Add(textTree.Branches[i].ToStringList());
                }
                i++;
            }

            // The original picked the data set by the component's RunCount (1 on a normal solve); Betta has no run count, so it is always the first.
            foreach (List<string> data in dataSets[0]) dataSet.Add(data);

            Block block = Block.CreateTable(dataSet);
            ApplyTableOptions(block, borderStyle, heading, columnFitting, alternatingColor);
            return block;
        }

        [GrasshopperMethod("Get Blocks", NickName = "Get Blk", Description = "Get the Blocks from Pages or Documents.",
            Guid = "ef1ea0e4-0378-462d-9ec2-58d579848650", IconResource = "Pdf-Block-Deconstruct.png")]
        [return: GrasshopperOutput("Block", "Bk", "PDF Document Block")]
        public List<Block> GetBlocks(
            [GrasshopperParameter("Page", "Pg", "A PDF Page")] object page)
        {
            Document document = new Document();
            if (page == null || !Wire.Goo(page).TryGetDocument(ref document)) return null;
            return document.Blocks;
        }

        [GrasshopperMethod("Set Blocks", NickName = "Set Blk", Description = "Sequentially place a list of Blocks into PDF Pages.",
            Guid = "d9c71778-1a53-443f-a3d3-cdc72ec5c2b1", IconResource = "Pdf-Block-Add.png")]
        [return: GrasshopperOutput("Document", "Dc", "A PDF Document")]
        public Document SetBlocks(
            [GrasshopperParameter("Page", "Pg", "A PDF Page", Optional = true)] object page,
            [GrasshopperParameter("Block", "Bk", "PDF Document Block", Optional = true)] List<object> blocks)
        {
            Page p = new Page();
            if (page != null) Wire.Goo(page).TryGetPage(ref p);

            if (blocks == null || blocks.Count == 0) return null;
            foreach (object b in blocks) p.AddBlock(Wire.Goo(b));

            return new Document(p);
        }

        private static List<Block> GetBlockList(List<object> goos)
        {
            List<Block> blocks = new List<Block>();
            if (goos == null) return blocks;
            foreach (object o in goos)
            {
                Block block = null;
                if (o != null && Wire.Goo(o).TryGetBlock(ref block)) blocks.Add(block);
            }
            return blocks;
        }

        private static void ApplyTableOptions(Block block, int border, int headers, int width, Sd.Color color)
        {
            switch (border)
            {
                default:
                    block.HorizontalBorderStyle = Element.BorderStyles.None;
                    block.VerticalBorderStyle = Element.BorderStyles.None;
                    break;
                case 1:
                    block.HorizontalBorderStyle = Element.BorderStyles.All;
                    block.VerticalBorderStyle = Element.BorderStyles.All;
                    break;
                case 2:
                    block.HorizontalBorderStyle = Element.BorderStyles.All;
                    block.VerticalBorderStyle = Element.BorderStyles.None;
                    break;
                case 3:
                    block.HorizontalBorderStyle = Element.BorderStyles.Interior;
                    block.VerticalBorderStyle = Element.BorderStyles.None;
                    break;
                case 4:
                    block.HorizontalBorderStyle = Element.BorderStyles.None;
                    block.VerticalBorderStyle = Element.BorderStyles.All;
                    break;
                case 5:
                    block.HorizontalBorderStyle = Element.BorderStyles.None;
                    block.VerticalBorderStyle = Element.BorderStyles.Interior;
                    break;
            }

            switch (headers)
            {
                default:
                    break;
                case 1:
                    block.XAxis = "Row";
                    break;
                case 2:
                    block.YAxis = "Column";
                    break;
                case 3:
                    block.XAxis = "Row";
                    block.YAxis = "Column";
                    break;
            }

            block.ColumnWidth = width;
            // Only a wired colour replaced the block's own alternate colour in the original.
            if (!color.IsEmpty) block.AlternateColor = color;
        }
    }
}
