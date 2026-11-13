using Betta.Attributes;
using Betta.Interfaces;
using PdfPlus.Betta;
using System;
using System.Collections.Generic;
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

        [GrasshopperMethod("Create Document", NickName = "Create Doc", Description = "Add a list of Pages to an existing PDF Document or create a new Document.",
            Guid = "c851d2f0-6304-46ea-89d7-b649b7d27594", IconResource = "Pdf-Document-Add.png")]
        [return: GrasshopperOutput("Document", "Dc", "A PDF Document")]
        public Document Create(
            [GrasshopperParameter("Document", "Dc", "A PDF Document", Optional = true)] object document,
            [GrasshopperParameter("Pages", "Pg", "Pages to add to the document")] List<object> pages)
        {
            Document doc = new Document();
            if (document != null) Wire.Goo(document).TryGetDocument(ref doc);

            if (pages == null || pages.Count == 0) return null;
            List<Page> list = new List<Page>();
            foreach (object p in pages)
            {
                Page page = new Page();
                if (Wire.Goo(p).TryGetPage(ref page)) list.Add(page);
            }

            doc.AddPages(list);
            return doc;
        }

        [GrasshopperMethod("Deconstruct Document", NickName = "De Doc", Description = "Deconstruct a PDF Document into its pages",
            Guid = "ebff998c-6d61-4430-a0cd-9e5d447c81ba", IconResource = "Pdf-Document-Deconstruct.png")]
        [return: GrasshopperOutput("Page", "Pg", "A PDF Page")]
        public List<Page> Deconstruct(
            [GrasshopperParameter("Document", "Dc", "A PDF Document")] object document)
        {
            Document doc = new Document();
            if (!Wire.Goo(document).TryGetDocument(ref doc)) return null;
            return doc.RenderBlocksToPages();
        }

        [GrasshopperMethod("Document MetaData", NickName = "Meta", Description = "Edit the PDF Document MetaData",
            Guid = "c3e354ad-50dc-42d0-ae7e-47201a854eec", IconResource = "Pdf-Document-Meta.png")]
        [return: GrasshopperOutput("Document", "Dc", "A PDF Document", Index = 0)]
        [return: GrasshopperOutput("Title", "T", "Concise title of the document", Index = 1)]
        [return: GrasshopperOutput("Subject", "S", "Brief summary of the document", Index = 2)]
        public (Document Document, string Title, string Subject) MetaData(
            [GrasshopperParameter("Document", "Dc", "A PDF Document")] object document,
            [GrasshopperParameter("Title", "T", "Concise title of the document \r\nNotes:\r\nThe title is not the filename and does not have to match.\r\nMany search engines use the title to describe the document in their search results list.", Optional = true)] string title,
            [GrasshopperParameter("Subject", "S", "Brief summary of the document", Optional = true)] string subject)
        {
            Document doc = new Document();
            if (!Wire.Goo(document).TryGetDocument(ref doc)) return default;

            if (title != null) doc.Title = title;
            if (subject != null) doc.Subject = subject;

            return (doc, doc.Title, doc.Subject);
        }

        // The original is titled "Document Password" / "Pass" / "Lock the PDF Document with a Password" (copied from the
        // password component); the name and description are kept as they were.
        [GrasshopperMethod("Document Password", NickName = "Pass", Description = "Lock the PDF Document with a Password",
            Guid = "ae62da1a-7db5-4fc4-9434-442c2369462d", IconResource = "Pdf-Document-Settings.png")]
        [return: GrasshopperOutput("Document", "Dc", "A PDF Document", Index = 0)]
        [return: GrasshopperOutput("Layout", "L", "Specifies the page layout to be used by a viewer when the document is opened.", Index = 1)]
        [return: GrasshopperOutput("Page", "P", "Specifies how the document should be displayed by a viewer when opened.", Index = 2)]
        [return: GrasshopperOutput("Color Mode", "C", "Specifies what color model is used in a PDF document.", Index = 3)]
        public (Document Document, int Layout, int Page, int ColorMode) Options(
            [GrasshopperParameter("Document", "Dc", "A PDF Document")] object document,
            [GrasshopperParameter("Layout", "L", "Specifies the page layout to be used by a viewer when the document is opened.", Optional = true, DefaultValue = (Document.LayoutModes)0)] Document.LayoutModes layout,
            [GrasshopperParameter("Page", "P", "Specifies how the document should be displayed by a viewer when opened.", Optional = true, DefaultValue = (Document.PageModes)0)] Document.PageModes page,
            [GrasshopperParameter("Color Mode", "C", "Specifies what color model is used in a PDF document.", Optional = true, DefaultValue = (Document.ColorModes)0)] Document.ColorModes colorMode)
        {
            Document doc = new Document();
            if (!Wire.Goo(document).TryGetDocument(ref doc)) return default;

            doc.LayoutMode = layout;
            doc.PageMode = page;
            doc.ColorMode = colorMode;

            return (doc, (int)doc.LayoutMode, (int)doc.PageMode, (int)doc.ColorMode);
        }

        [GrasshopperMethod("Document Password", NickName = "Pass", Description = "Lock the PDF Document with a Password",
            Guid = "df83b610-7553-4857-8f86-defb4800a0ea", IconResource = "Pdf-Document-Password.png")]
        [return: GrasshopperOutput("Document", "Dc", "A PDF Document")]
        public Document Password(
            [GrasshopperParameter("Document", "Dc", "A PDF Document")] object document,
            [GrasshopperParameter("Password", "P", "Password to Lock the ", Optional = true)] string password)
        {
            Document doc = new Document();
            if (!Wire.Goo(document).TryGetDocument(ref doc)) return null;

            if (password != null) doc.Password = password;
            return doc;
        }

        // Permissions are only changed when wired in the original; unwired inputs here take the engine's own defaults (true).
        [GrasshopperMethod("Document Permissions", NickName = "Permissions", Description = "Set the PDF Document Access Permissions",
            Guid = "0aaa0787-69f0-41a5-95a3-faf1fe694286", IconResource = "Pdf-Document-Permissions.png")]
        [return: GrasshopperOutput("Document", "Dc", "A PDF Document", Index = 0)]
        [return: GrasshopperOutput("Modify", "M", "Allow users to Modify the PDF", Index = 1)]
        [return: GrasshopperOutput("Extract", "E", "Allow users to Extract from the PDF", Index = 2)]
        [return: GrasshopperOutput("Print", "P", "Allow users to Print the Document", Index = 3)]
        public (Document Document, bool Modify, bool Extract, bool Print) Permissions(
            [GrasshopperParameter("Document", "Dc", "A PDF Document")] object document,
            [GrasshopperParameter("Modify", "M", "Allow users to Modify the PDF", Optional = true, DefaultValue = true)] bool modify,
            [GrasshopperParameter("Extract", "E", "Allow users to Extract from the PDF", Optional = true, DefaultValue = true)] bool extract,
            [GrasshopperParameter("Print", "P", "Allow users to Print the Document", Optional = true, DefaultValue = true)] bool print,
            [GrasshopperParameter("Optional Password", "O", "Optional Password to let users override permissions.\r\nIf no value is provided a random GUID will be assigned.\r\nNote: This password does not limit access to the PDF.", Optional = true)] bool ownerPassword)
        {
            Document doc = new Document();
            if (!Wire.Goo(document).TryGetDocument(ref doc)) return default;

            doc.PermitModify = modify;
            doc.PermitExtract = extract;
            doc.PermitPrint = print;
            // The original assigns PermitPrint = print again when the password input is wired, and never sets a password.

            return (doc, doc.PermitModify, doc.PermitExtract, doc.PermitPrint);
        }

        // Nothing to compute: Document draws itself in the viewport, which is what SetPreview did.
        [GrasshopperMethod("Preview PDF", NickName = "Prev PDF", Description = "Preview PDF Shapes, Pages, or Documents.",
            Guid = "d9baa330-a6ee-4e7a-9ddc-a4491d2be7af", IconResource = "Pdf-Document-Preview.png")]
        public void Preview(
            [GrasshopperParameter("Element", "El", "A PDF+ Shape, Block, DataSet, Text Fragment Element, or Geometry")] object element)
        {
            Document doc = new Document();
            Wire.Goo(element).TryGetDocument(ref doc);
        }
    }
}
