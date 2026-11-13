using Betta.Attributes;
using Betta.Interfaces;
using PdfPlus.Betta;

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
    }
}
