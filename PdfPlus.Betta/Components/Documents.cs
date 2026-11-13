using Betta.Attributes;
using Betta.Interfaces;
using PdfPlus.Betta;
using System;
using System.IO;

namespace PdfPlus.Components
{
    [GrasshopperCollection("PDF", "Doc")]
    public class Documents : IBettaCollection
    {
        [GrasshopperMethod("Save Document", NickName = "Save Doc", Description = "Save the PDF Document to a local file",
            Guid = "0633f5dc-c0a4-4399-9ead-ab58467cec2e", IconResource = "Pdf-Document-Save.png")]
        [return: GrasshopperOutput("File Path", "P", "The full file path to the saved file.")]
        public string Save(
            [GrasshopperParameter("Document", "Dc", "A PDF Document")] object document,
            [GrasshopperParameter("Directory", "D", "The directory to save the Document.", Optional = true)] string directory,
            [GrasshopperParameter("File Name", "N", "The Document name to save.", Optional = true)] string fileName,
            [GrasshopperParameter("Save", "_S", "Toggle to true to save the Document.", Optional = true, DefaultValue = false)] bool save,
            IBettaReport report)
        {
            Document doc = new Document();
            if (!Wire.Goo(document).TryGetDocument(ref doc)) return null;
            if (!save) return null;

            string path = directory;
            if (string.IsNullOrEmpty(path))
            {
                // The original fell back to the .gh file's folder, then to Public Documents.
                string ghFile = Grasshopper.Instances.ActiveCanvas?.Document?.FilePath;
                path = ghFile != null ? Path.GetDirectoryName(ghFile) : "C:\\Users\\Public\\Documents\\";
            }
            if (!Directory.Exists(path))
            {
                report.Warning("The provided directory does not exist. Please verify this is a valid file path.");
                return null;
            }

            string name = string.IsNullOrEmpty(fileName) ? DateTime.UtcNow.ToString("yyyy-dd-M_HH-mm-ss") : fileName;
            name = name.EndsWith(".pdf") ? name : $"{name}.pdf";
            string filepath = Path.Combine(path, name);
            doc.Save(filepath);
            return filepath;
        }
    }
}
