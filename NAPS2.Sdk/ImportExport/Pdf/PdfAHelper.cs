﻿using System.Text;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.Advanced;

namespace NAPS2.ImportExport.Pdf;

public static class PdfAHelper
{
    public static void CreateXmpMetadata(PdfDocument document, PdfCompat compat)
    {
        var metadataDict = new PdfDictionary(document);
        metadataDict.Elements["/Type"] = new PdfName("/Metadata");
        metadataDict.Elements["/Subtype"] = new PdfName("/XML");
        metadataDict.CreateStream(CreateRawXmpMetadata(document.Info, GetConformance(compat)));
        document.Internals.AddObject(metadataDict);
        document.Internals.Catalog.Elements["/Metadata"] = metadataDict.Reference;
    }

    private static (string, string) GetConformance(PdfCompat compat)
    {
        switch (compat)
        {
            case PdfCompat.PdfA1B:
                return ("1", "B");
            case PdfCompat.PdfA2B:
                return ("2", "B");
            case PdfCompat.PdfA3B:
                return ("3", "B");
            case PdfCompat.PdfA3U:
                return ("3", "U");
            default:
                return ("", "");
        }
    }

    private static byte[] CreateRawXmpMetadata(PdfDocumentInformation info, (string, string) conformance)
    {
        string xml = $@"<?xpacket begin=""{'\ufeff'}"" id=""W5M0MpCehiHzreSzNTczkc9d""?>
<x:xmpmeta xmlns:x=""adobe:ns:meta/"" x:xmptk=""Adobe XMP Core 5.1.0-jc003"">
  <rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#"">
    <rdf:Description rdf:about=""""
        xmlns:dc=""http://purl.org/dc/elements/1.1/""
        xmlns:pdf=""http://ns.adobe.com/pdf/1.3/""
        xmlns:xmp=""http://ns.adobe.com/xap/1.0/""
        xmlns:pdfaid=""http://www.aiim.org/pdfa/ns/id/""
      dc:format=""application/pdf""
	  pdf:Keywords=""{info.Keywords}""
      pdf:Producer=""{PdfSharpCore.ProductVersionInfo.Producer}""
      xmp:CreateDate=""{info.CreationDate:yyyy'-'MM'-'dd'T'HH':'mm':'ssK}""
      xmp:ModifyDate=""{info.ModificationDate:yyyy'-'MM'-'dd'T'HH':'mm':'ssK}""
      xmp:CreatorTool=""{info.Creator}""
      pdfaid:part=""{ conformance.Item1 }""
      pdfaid:conformance=""{ conformance.Item2 }"">
	  <dc:creator>
        <rdf:Seq>
          <rdf:li>{info.Author}</rdf:li>
        </rdf:Seq>
      </dc:creator>
      <dc:title>
        <rdf:Alt>
          <rdf:li xml:lang=""x-default"">{info.Title}</rdf:li>
        </rdf:Alt>
      </dc:title>
      <dc:subject>
        <rdf:Bag>
          <rdf:li>{info.Keywords}</rdf:li>
        </rdf:Bag>
      </dc:subject>
      <dc:description>
        <rdf:Alt>
          <rdf:li xml:lang=""x-default"">{info.Subject}</rdf:li>
        </rdf:Alt>
      </dc:description>
	  </rdf:Description>
  </rdf:RDF>
</x:xmpmeta>                                                                            
<?xpacket end=""w""?>";
        return Encoding.UTF8.GetBytes(xml);
    }

    public static void DisableTransparency(PdfDocument document)
    {
        document.Options.ColorMode = PdfColorMode.Undefined;
    }

    public static void SetColorProfile(PdfDocument document)
    {
        var iccProfile = new PdfDictionary(document);
        iccProfile.Elements["/Alternate"] = new PdfName("/DeviceRGB");
        iccProfile.Elements["/N"] = new PdfInteger(3);
        iccProfile.Elements["/Filter"] = new PdfName("/FlateDecode");
        iccProfile.CreateStream(IccPofileBytes);
        document.Internals.AddObject(iccProfile);

        var intent = new PdfDictionary(document);
        intent.Elements["/Type"] = new PdfName("/OutputIntent");
        intent.Elements["/S"] = new PdfName("/GTS_PDFA1");
        intent.Elements["/OutputConditionIdentifier"] = new PdfString("sRGB IEC61966-2.1");
        intent.Elements["/Info"] = new PdfString("sRGB IEC61966-2.1");
        intent.Elements["/DestOutputProfile"] = iccProfile.Reference;
        document.Internals.Catalog.Elements["/OutputIntents"] = new PdfArray(document, intent);
    }

    public static void SetCidStream(PdfDocument document)
    {
        var cidStream = new PdfDictionary(document);
        cidStream.CreateStream(new byte[] { 0 });
        document.Internals.AddObject(cidStream);

        foreach (var font in document.Internals.GetAllObjects().OfType<PdfDictionary>())
        {
            var type = font.Elements["/Type"] as PdfName;
            if (type != null && type.Value == "/FontDescriptor")
            {
                font.Elements["/CIDSet"] = cidStream.Reference;
            }
        }
    }

    public static void SetCidMap(PdfDocument document)
    {
        foreach (var font in document.Internals.GetAllObjects().OfType<PdfFont>())
        {
            var subtype = font.Elements["/Subtype"] as PdfName;
            if (subtype != null && subtype.Value.StartsWith("/CID", StringComparison.InvariantCulture))
            {
                font.Elements["/CIDToGIDMap"] = new PdfName("/Identity");
            }
        }
    }

    private static readonly byte[] IccPofileBytes = { 0x78, 0x9C, 0xB5, 0x96, 0x69, 0x50, 0x13, 0xD9, 0x16, 0xC7, 0x6F, 0x77, 0xF6, 0x8D, 0x2D, 0x01, 0x01, 0x59, 0xC2, 0xBE, 0x86, 0x4D, 0x96, 0x00, 0xB2, 0x86, 0x2D, 0xA0, 0x82, 0x80, 0x6C, 0xA2, 0x12, 0x92, 0x00, 0x61, 0x09, 0x10, 0x12, 0xC0, 0x5D, 0x11, 0x51, 0x81, 0x11, 0x45, 0x44, 0x04, 0x5C, 0x40, 0x06, 0x45, 0x1C, 0x70, 0x74, 0x58, 0x64, 0x10, 0x11, 0x51, 0xDC, 0x06, 0x05, 0x05, 0x54, 0x5C, 0x82, 0x0C, 0x0A, 0xCA, 0x38, 0x38, 0x8A, 0x1B, 0x2A, 0xAF, 0x03, 0x1F, 0x74, 0xEA, 0x4D, 0xD5, 0xAB, 0x57, 0xAF, 0xDE, 0xBF, 0xAA, 0xBB, 0x7F, 0x7D, 0xEA, 0x9C, 0xDB, 0xE7, 0x9E, 0xFB, 0xA1, 0xFF, 0x00,
        0x90, 0x6E, 0x03, 0x44, 0x30, 0x72, 0xA5, 0x08, 0xC5, 0xA2, 0x60, 0x5F, 0x4F, 0x7A, 0x44, 0x64, 0x14, 0x1D, 0xF7, 0x04, 0x09, 0xC9, 0x83, 0x39, 0x71, 0xB8, 0x19, 0x69, 0xE0, 0xDF, 0x05, 0x7D, 0xC3, 0x77, 0x83, 0xF3, 0x6F, 0x77, 0x18, 0xB2, 0xBB, 0x8B, 0xDC, 0x99, 0xC7, 0x0D, 0xFB, 0x0E, 0x74, 0x54, 0xC4, 0x1B, 0xF6, 0x16, 0x12, 0x59, 0xBD, 0xFF, 0x50, 0xFB, 0xBD, 0x14, 0x78, 0xFC, 0x0C, 0x2E, 0xB2, 0x9C, 0x17, 0xC2, 0xDC, 0x58, 0xE4, 0xE3, 0x08, 0x9F, 0x40, 0x98, 0x16, 0x1B, 0x1A, 0xCC, 0x42, 0xB8, 0x1D, 0x00, 0x3C, 0x39, 0xFE, 0x3B, 0x16, 0x7D, 0xC7, 0xBC, 0x14, 0x1E, 0x0F, 0x00, 0xC2, 0x55, 0x24, 0x7F, 0x67, 0xFC, 0x5C, 0x2D, 0x31, 0x48,
        0x56, 0x9B, 0x2C, 0x49, 0x11, 0x20, 0xCC, 0x93, 0x71, 0x0A, 0x9F, 0x93, 0x81, 0x70, 0x36, 0xC2, 0xFA, 0xB1, 0x49, 0x69, 0x62, 0x84, 0x2B, 0x65, 0x71, 0xD1, 0x7C, 0xFE, 0x69, 0x19, 0x8B, 0xF9, 0xDC, 0x04, 0x84, 0xBB, 0x11, 0x26, 0x67, 0x4A, 0xF8, 0x48, 0x1D, 0x51, 0x36, 0x97, 0x1D, 0x59, 0x62, 0x59, 0x3E, 0x29, 0x42, 0x96, 0xC3, 0x4D, 0x13, 0xC9, 0x38, 0x19, 0x61, 0x07, 0x6E, 0x02, 0x07, 0xC9, 0x21, 0xD5, 0x20, 0x6C, 0x3E, 0xDF, 0xFF, 0x9C, 0x08, 0x19, 0xC8, 0x00, 0x6D, 0xAC, 0xAC, 0x6D, 0xFF, 0xC3, 0x9E, 0xFF, 0x6B, 0xC9, 0xCE, 0x64, 0x9E, 0xF4, 0x8F, 0x02, 0x20, 0x9F, 0x0B, 0x40, 0xF3, 0x2D, 0xAE, 0x44, 0x94, 0x39, 0x1F, 0x43, 0xCB, 0x6E,
        0x18, 0x40, 0x44, 0x4E, 0x8C, 0x06, 0xD4, 0x80, 0x16, 0xD0, 0x03, 0xC6, 0x80, 0x01, 0x6C, 0x80, 0x03, 0x70, 0x06, 0xEE, 0xC0, 0x1B, 0xF8, 0x83, 0x40, 0x10, 0x0A, 0x22, 0xC1, 0x6A, 0xC0, 0x05, 0x09, 0x20, 0x05, 0x88, 0x40, 0x16, 0xD8, 0x00, 0xB6, 0x82, 0x3C, 0x50, 0x00, 0xF6, 0x80, 0xFD, 0xA0, 0x1C, 0x1C, 0x01, 0x35, 0xA0, 0x0E, 0x34, 0x80, 0x33, 0xA0, 0x05, 0xB4, 0x83, 0x8B, 0xE0, 0x0A, 0xB8, 0x01, 0x6E, 0x83, 0x01, 0x30, 0x0C, 0xA4, 0x60, 0x0C, 0xBC, 0x04, 0x53, 0xE0, 0x1D, 0x98, 0x81, 0x20, 0x08, 0x07, 0x51, 0x20, 0x2A, 0xA4, 0x06, 0x69, 0x43, 0x06, 0x90, 0x19, 0x64, 0x03, 0x31, 0x21, 0x57, 0xC8, 0x1B, 0x5A, 0x02, 0x05, 0x43, 0x91, 0x50,
        0x0C, 0x14, 0x0F, 0x09, 0x21, 0x09, 0xB4, 0x01, 0xDA, 0x06, 0x15, 0x40, 0xC5, 0x50, 0x39, 0x54, 0x05, 0xD5, 0x41, 0x3F, 0x43, 0xE7, 0xA0, 0x8B, 0xD0, 0x35, 0xA8, 0x0F, 0xBA, 0x0F, 0x8D, 0x40, 0x13, 0xD0, 0x5F, 0xD0, 0x27, 0x18, 0x05, 0x93, 0x61, 0x1A, 0xAC, 0x09, 0x1B, 0xC2, 0x96, 0x30, 0x13, 0xF6, 0x80, 0x03, 0xE0, 0x50, 0x78, 0x15, 0x1C, 0x0F, 0xA7, 0xC3, 0xEB, 0xE0, 0x5C, 0x78, 0x37, 0x5C, 0x06, 0x57, 0xC3, 0xA7, 0xE0, 0x66, 0xF8, 0x22, 0x7C, 0x03, 0x1E, 0x80, 0xA5, 0xF0, 0x4B, 0x78, 0x1A, 0x05, 0x50, 0x24, 0x94, 0x0A, 0x4A, 0x07, 0xC5, 0x40, 0x31, 0x51, 0x2C, 0x54, 0x20, 0x2A, 0x0A, 0x15, 0x87, 0x12, 0xA1, 0x36, 0xA1, 0xF2, 0x51, 0xA5,
        0xA8, 0x6A, 0x54, 0x03, 0xAA, 0x0D, 0xD5, 0x83, 0xBA, 0x83, 0x92, 0xA2, 0x26, 0x51, 0x1F, 0xD1, 0x58, 0x34, 0x15, 0x4D, 0x47, 0x33, 0xD0, 0xCE, 0x68, 0x3F, 0xF4, 0x0A, 0x34, 0x17, 0x9D, 0x8E, 0xDE, 0x84, 0x2E, 0x44, 0x97, 0xA3, 0x4F, 0xA0, 0x9B, 0xD1, 0xDD, 0xE8, 0x3B, 0xE8, 0x11, 0xF4, 0x14, 0xFA, 0x2B, 0x86, 0x82, 0xD1, 0xC0, 0x98, 0x61, 0x9C, 0x30, 0x6C, 0x4C, 0x04, 0x26, 0x1E, 0x93, 0x85, 0xC9, 0xC3, 0x94, 0x62, 0x6A, 0x31, 0x4D, 0x98, 0xCB, 0x98, 0x01, 0xCC, 0x18, 0xE6, 0x1D, 0x16, 0x8B, 0x55, 0xC1, 0x1A, 0x61, 0x1D, 0xB0, 0x7E, 0xD8, 0x48, 0x6C, 0x22, 0x76, 0x3D, 0xB6, 0x10, 0x7B, 0x08, 0xDB, 0x88, 0xED, 0xC4, 0xF6, 0x61, 0x47, 0xB1,
        0xD3, 0x38, 0x1C, 0x4E, 0x0D, 0x67, 0x86, 0x73, 0xC1, 0x05, 0xE2, 0x38, 0x38, 0x31, 0x2E, 0x0F, 0x77, 0x10, 0x77, 0x0A, 0x77, 0x01, 0xD7, 0x8F, 0x1B, 0xC3, 0x7D, 0xC0, 0x93, 0xF0, 0xDA, 0x78, 0x1B, 0xBC, 0x0F, 0x3E, 0x0A, 0x2F, 0xC4, 0xE7, 0xE0, 0x4B, 0xF1, 0x27, 0xF1, 0x1D, 0xF8, 0x7E, 0xFC, 0x73, 0xFC, 0x0C, 0x41, 0x81, 0x60, 0x40, 0x70, 0x22, 0x04, 0x12, 0x78, 0x84, 0xB5, 0x84, 0x22, 0x42, 0x0D, 0xA1, 0x8D, 0x70, 0x8B, 0x30, 0x46, 0x98, 0x21, 0x2A, 0x12, 0x8D, 0x88, 0x2E, 0xC4, 0x50, 0x62, 0x22, 0x71, 0x2B, 0xB1, 0x8C, 0xD8, 0x40, 0xBC, 0x4C, 0x7C, 0x48, 0x7C, 0x43, 0x22, 0x91, 0x74, 0x49, 0x8E, 0xA4, 0xE5, 0x24, 0x01, 0x69, 0x0B, 0xA9,
        0x8C, 0x74, 0x9A, 0x74, 0x95, 0x34, 0x42, 0xFA, 0x48, 0x56, 0x22, 0x9B, 0x92, 0x59, 0xE4, 0x68, 0xB2, 0x84, 0xBC, 0x9B, 0x7C, 0x9C, 0xDC, 0x49, 0xBE, 0x4F, 0x7E, 0x43, 0xA1, 0x50, 0x0C, 0x29, 0xEE, 0x94, 0x28, 0x8A, 0x98, 0xB2, 0x9B, 0x52, 0x47, 0xB9, 0x44, 0x79, 0x4C, 0xF9, 0x20, 0x47, 0x95, 0xB3, 0x90, 0x63, 0xCB, 0xF1, 0xE4, 0x36, 0xCB, 0x55, 0xC8, 0x35, 0xCB, 0xF5, 0xCB, 0xBD, 0x92, 0x27, 0xC8, 0x1B, 0xC8, 0x7B, 0xC8, 0xAF, 0x96, 0x5F, 0x27, 0x5F, 0x2A, 0x7F, 0x56, 0xFE, 0x96, 0xFC, 0xA4, 0x02, 0x41, 0xC1, 0x50, 0x81, 0xA5, 0xC0, 0x51, 0xD8, 0xA4, 0x50, 0xA1, 0x70, 0x4E, 0x61, 0x48, 0x61, 0x5A, 0x91, 0xAA, 0x68, 0xAD, 0x18, 0xA8, 0x98,
        0xA2, 0x58, 0xA8, 0x78, 0x52, 0xF1, 0x9A, 0xE2, 0xB8, 0x12, 0x4E, 0xC9, 0x50, 0xC9, 0x5B, 0x89, 0xA7, 0x94, 0xAB, 0x74, 0x4C, 0xE9, 0x92, 0xD2, 0x28, 0x15, 0x45, 0xD5, 0xA3, 0xB2, 0xA8, 0x5C, 0xEA, 0x36, 0x6A, 0x0D, 0xF5, 0x32, 0x75, 0x8C, 0x86, 0xA5, 0x19, 0xD1, 0xD8, 0xB4, 0x44, 0x5A, 0x01, 0xED, 0x27, 0x5A, 0x2F, 0x6D, 0x4A, 0x59, 0x49, 0xD9, 0x56, 0x39, 0x4C, 0x39, 0x5B, 0xB9, 0x42, 0xF9, 0xBC, 0xB2, 0x54, 0x05, 0xA5, 0x62, 0xA8, 0xC2, 0x56, 0x49, 0x56, 0x29, 0x52, 0x39, 0xA3, 0x32, 0xA8, 0xF2, 0x69, 0x81, 0xE6, 0x02, 0x8F, 0x05, 0xFC, 0x05, 0xBB, 0x16, 0x34, 0x2C, 0xE8, 0x5F, 0xF0, 0x5E, 0x75, 0xA1, 0xAA, 0xBB, 0x2A, 0x5F, 0x35, 0x5F,
        0xB5, 0x51, 0x75, 0x40, 0xF5, 0x93, 0x1A, 0x5D, 0xCD, 0x5B, 0x2D, 0x49, 0x6D, 0xAF, 0x5A, 0x8B, 0xDA, 0x23, 0x75, 0xB4, 0xBA, 0xA9, 0xFA, 0x72, 0xF5, 0x2C, 0xF5, 0xC3, 0xEA, 0x97, 0xD5, 0x27, 0x17, 0xD2, 0x16, 0x3A, 0x2F, 0xE4, 0x2E, 0xCC, 0x5F, 0x78, 0x66, 0xE1, 0x03, 0x0D, 0x58, 0xC3, 0x54, 0x23, 0x58, 0x63, 0xBD, 0xC6, 0x31, 0x8D, 0x9B, 0x1A, 0xD3, 0x9A, 0x5A, 0x9A, 0xBE, 0x9A, 0x69, 0x9A, 0x07, 0x35, 0x2F, 0x69, 0x4E, 0x6A, 0xA9, 0x68, 0xB9, 0x6B, 0x25, 0x6A, 0x95, 0x68, 0x75, 0x68, 0x4D, 0x68, 0x53, 0xB5, 0x5D, 0xB5, 0x05, 0xDA, 0x25, 0xDA, 0x17, 0xB4, 0x5F, 0xD0, 0x95, 0xE9, 0x1E, 0xF4, 0x64, 0x7A, 0x19, 0xBD, 0x9B, 0x3E, 0xA5, 0xA3,
        0xA1, 0xE3, 0xA7, 0x23, 0xD1, 0xA9, 0xD2, 0xE9, 0xD5, 0x99, 0xD1, 0x35, 0xD2, 0x5D, 0xA1, 0x9B, 0xA3, 0xDB, 0xA8, 0xFB, 0x48, 0x8F, 0xA8, 0xC7, 0xD4, 0x8B, 0xD3, 0x2B, 0xD1, 0xEB, 0xD2, 0x9B, 0xD2, 0xD7, 0xD6, 0x5F, 0xAA, 0xBF, 0x41, 0xBF, 0x5E, 0xFF, 0x81, 0x01, 0xC1, 0x80, 0x69, 0x90, 0x60, 0x70, 0xC0, 0xA0, 0xC7, 0xE0, 0xBD, 0xA1, 0x91, 0x61, 0xB8, 0xE1, 0x0E, 0xC3, 0x16, 0xC3, 0x71, 0x23, 0x55, 0x23, 0xB6, 0xD1, 0x3A, 0xA3, 0x7A, 0xA3, 0x87, 0xC6, 0x14, 0x63, 0x37, 0xE3, 0x74, 0xE3, 0x6A, 0xE3, 0xBB, 0x26, 0x58, 0x13, 0xA6, 0x49, 0x92, 0xC9, 0x21, 0x93, 0xDB, 0xA6, 0xB0, 0xA9, 0x9D, 0x69, 0x82, 0x69, 0x85, 0xE9, 0x2D, 0x33, 0xD8, 0xCC,
        0xDE, 0x4C, 0x60, 0x76, 0xC8, 0xAC, 0xCF, 0x1C, 0x63, 0xEE, 0x68, 0x2E, 0x34, 0xAF, 0x36, 0x1F, 0x62, 0x90, 0x19, 0x1E, 0x8C, 0x4C, 0x46, 0x3D, 0x63, 0xC4, 0x42, 0xC5, 0x62, 0x89, 0x45, 0x8E, 0x45, 0x8B, 0xC5, 0x2B, 0x4B, 0x7D, 0xCB, 0x28, 0xCB, 0xBD, 0x96, 0x3D, 0x96, 0x5F, 0xAD, 0xEC, 0xAC, 0x92, 0xAD, 0x6A, 0xAC, 0x86, 0xAD, 0x95, 0xAC, 0xFD, 0xAD, 0x73, 0xAC, 0xDB, 0xAC, 0xFF, 0xB2, 0x31, 0xB5, 0xE1, 0xDA, 0x54, 0xD8, 0xDC, 0x5D, 0x44, 0x59, 0xE4, 0xB3, 0x68, 0xF3, 0xA2, 0xD6, 0x45, 0xAF, 0x6D, 0xCD, 0x6C, 0xF9, 0xB6, 0x87, 0x6D, 0xEF, 0xD9, 0x51, 0xED, 0x96, 0xDA, 0xED, 0xB0, 0xEB, 0xB2, 0xFB, 0x62, 0xEF, 0x60, 0x2F, 0xB2, 0x6F, 0xB0,
        0x9F, 0x70, 0xD0, 0x77, 0x88, 0x71, 0xA8, 0x74, 0x18, 0x62, 0xD2, 0x98, 0x41, 0xCC, 0x42, 0xE6, 0x55, 0x47, 0x8C, 0xA3, 0xA7, 0xE3, 0x66, 0xC7, 0x76, 0xC7, 0x8F, 0x4E, 0xF6, 0x4E, 0x62, 0xA7, 0x33, 0x4E, 0x7F, 0x3A, 0x33, 0x9C, 0x93, 0x9C, 0x4F, 0x3A, 0x8F, 0x2F, 0x36, 0x5A, 0xCC, 0x5F, 0x5C, 0xB3, 0x78, 0xD4, 0x45, 0xD7, 0x85, 0xE3, 0x52, 0xE5, 0x22, 0x75, 0xA5, 0xBB, 0xC6, 0xB8, 0x1E, 0x75, 0x95, 0xBA, 0xE9, 0xB8, 0x71, 0xDC, 0xAA, 0xDD, 0x9E, 0xBA, 0xEB, 0xB9, 0xF3, 0xDC, 0x6B, 0xDD, 0x9F, 0x7B, 0x98, 0x78, 0x24, 0x7A, 0x9C, 0xF2, 0x78, 0xE5, 0x69, 0xE5, 0x29, 0xF2, 0x6C, 0xF2, 0x7C, 0xCF, 0x72, 0x62, 0x6D, 0x64, 0x75, 0x7A, 0xA1, 0xBC,
        0x7C, 0xBD, 0xF2, 0xBD, 0x7A, 0xBD, 0x95, 0xBC, 0x57, 0x78, 0x97, 0x7B, 0x3F, 0xF6, 0xD1, 0xF5, 0x89, 0xF7, 0xA9, 0xF7, 0x99, 0xF2, 0xB5, 0xF3, 0x5D, 0xEF, 0xDB, 0xE9, 0x87, 0xF1, 0x0B, 0xF0, 0xDB, 0xEB, 0x37, 0xC4, 0xD6, 0x64, 0x73, 0xD9, 0x75, 0xEC, 0x29, 0x7F, 0x07, 0xFF, 0x8D, 0xFE, 0xDD, 0x01, 0xE4, 0x80, 0x90, 0x80, 0xF2, 0x80, 0xA7, 0x4B, 0x4C, 0x97, 0x88, 0x96, 0xB4, 0x2D, 0x85, 0x97, 0xFA, 0x2F, 0xDD, 0xB7, 0xF4, 0xE1, 0x32, 0x83, 0x65, 0xC2, 0x65, 0x2D, 0x81, 0x20, 0x90, 0x1D, 0xB8, 0x2F, 0xF0, 0x51, 0x90, 0x51, 0x50, 0x7A, 0xD0, 0xAF, 0xCB, 0xB1, 0xCB, 0x83, 0x96, 0x57, 0x2C, 0x7F, 0x16, 0x6C, 0x1D, 0xBC, 0x21, 0xB8, 0x27, 0x84,
        0x1A, 0xB2, 0x26, 0xE4, 0x64, 0xC8, 0xBB, 0x50, 0xCF, 0xD0, 0xA2, 0xD0, 0xE1, 0x15, 0xC6, 0x2B, 0x24, 0x2B, 0xBA, 0xC2, 0xE4, 0xC3, 0xA2, 0xC3, 0xEA, 0xC2, 0xDE, 0x87, 0x7B, 0x85, 0x17, 0x87, 0x4B, 0x23, 0x2C, 0x23, 0x36, 0x46, 0xDC, 0x88, 0x54, 0x8F, 0x14, 0x44, 0xB6, 0x46, 0xE1, 0xA2, 0xC2, 0xA2, 0x6A, 0xA3, 0xA6, 0x57, 0x7A, 0xAF, 0xDC, 0xBF, 0x72, 0x2C, 0xDA, 0x2E, 0x3A, 0x2F, 0x7A, 0x70, 0x95, 0xD1, 0xAA, 0xEC, 0x55, 0xD7, 0x56, 0xAB, 0xAF, 0x4E, 0x5E, 0x7D, 0x7E, 0x8D, 0xFC, 0x1A, 0xCE, 0x9A, 0xB3, 0x31, 0x98, 0x98, 0xF0, 0x98, 0x93, 0x31, 0x9F, 0x39, 0x81, 0x9C, 0x6A, 0xCE, 0x74, 0x2C, 0x3B, 0xB6, 0x32, 0x76, 0x8A, 0xCB, 0xE2, 0x1E,
        0xE0, 0xBE, 0xE4, 0xB9, 0xF3, 0x4A, 0x78, 0x13, 0x7C, 0x17, 0x7E, 0x31, 0xFF, 0x79, 0x9C, 0x4B, 0x5C, 0x71, 0xDC, 0x78, 0xBC, 0x4B, 0xFC, 0xBE, 0xF8, 0x89, 0x04, 0xB7, 0x84, 0xD2, 0x84, 0x49, 0x01, 0x4B, 0x50, 0x2E, 0x78, 0x9D, 0xE8, 0x97, 0x78, 0x24, 0xF1, 0x7D, 0x52, 0x60, 0xD2, 0xF1, 0xA4, 0xD9, 0xE4, 0xF0, 0xE4, 0xC6, 0x14, 0x7C, 0x4A, 0x4C, 0xCA, 0x39, 0xA1, 0x92, 0x30, 0x49, 0xD8, 0x9D, 0xAA, 0x95, 0x9A, 0x9D, 0xDA, 0x97, 0x66, 0x96, 0x96, 0x97, 0x26, 0x4D, 0x77, 0x4A, 0xDF, 0x9F, 0x3E, 0x25, 0x0A, 0x10, 0xD5, 0x66, 0x40, 0x19, 0xAB, 0x32, 0x5A, 0xC5, 0x34, 0xE4, 0x47, 0x72, 0x53, 0x62, 0x2C, 0xD9, 0x2E, 0x19, 0xC9, 0x74, 0xCD, 0xAC,
        0xC8, 0xFC, 0x90, 0x15, 0x96, 0x75, 0x36, 0x5B, 0x31, 0x5B, 0x98, 0x7D, 0x73, 0xAD, 0xE9, 0xDA, 0x5D, 0x6B, 0x9F, 0xAF, 0xF3, 0x59, 0xF7, 0xE3, 0x7A, 0xF4, 0x7A, 0xEE, 0xFA, 0xAE, 0x0D, 0x3A, 0x1B, 0xB6, 0x6E, 0x18, 0xD9, 0xE8, 0xB1, 0xB1, 0x6A, 0x13, 0xB4, 0x29, 0x76, 0x53, 0xD7, 0x66, 0xBD, 0xCD, 0xB9, 0x9B, 0xC7, 0xB6, 0xF8, 0x6E, 0x39, 0xB1, 0x95, 0xB8, 0x35, 0x69, 0xEB, 0x6F, 0x39, 0x56, 0x39, 0xC5, 0x39, 0x6F, 0xB7, 0x85, 0x6F, 0x6B, 0xCB, 0xD5, 0xCC, 0xDD, 0x92, 0x3B, 0xBA, 0xDD, 0x77, 0x7B, 0x7D, 0x9E, 0x5C, 0x9E, 0x28, 0x6F, 0x68, 0x87, 0xF3, 0x8E, 0x23, 0x3B, 0xD1, 0x3B, 0x05, 0x3B, 0x7B, 0x77, 0x2D, 0xDA, 0x75, 0x70, 0xD7, 0xD7,
        0x7C, 0x5E, 0xFE, 0xF5, 0x02, 0xAB, 0x82, 0xD2, 0x82, 0xCF, 0x85, 0xDC, 0xC2, 0xEB, 0x3F, 0x58, 0xFF, 0x50, 0xF6, 0xC3, 0xEC, 0xEE, 0xB8, 0xDD, 0xBD, 0x45, 0xF6, 0x45, 0x87, 0xF7, 0x60, 0xF7, 0x08, 0xF7, 0x0C, 0xEE, 0x75, 0xDB, 0x7B, 0xA2, 0x58, 0xB1, 0x78, 0x5D, 0xF1, 0xE8, 0xBE, 0xA5, 0xFB, 0x9A, 0x4B, 0xE8, 0x25, 0xF9, 0x25, 0x6F, 0xF7, 0xAF, 0xD9, 0x7F, 0xAD, 0xD4, 0xB6, 0xF4, 0xC8, 0x01, 0xE2, 0x01, 0xC9, 0x01, 0x69, 0xD9, 0x92, 0xB2, 0xD6, 0x83, 0xFA, 0x07, 0xF7, 0x1C, 0xFC, 0x5C, 0x9E, 0x50, 0x3E, 0x50, 0xE1, 0x59, 0xD1, 0x58, 0xA9, 0x51, 0xB9, 0xAB, 0xF2, 0xFD, 0x21, 0xDE, 0xA1, 0xFE, 0xC3, 0xEE, 0x87, 0x1B, 0x8E, 0x68, 0x1E, 0x29,
        0x38, 0xF2, 0xE9, 0xA8, 0xE0, 0xE8, 0xBD, 0x2A, 0xDF, 0xAA, 0xE6, 0x6A, 0xC3, 0xEA, 0xD2, 0x63, 0xD8, 0x63, 0x99, 0xC7, 0x9E, 0xD5, 0x84, 0xD5, 0xF4, 0xFC, 0xC8, 0xFC, 0xB1, 0xAE, 0x56, 0xBD, 0xB6, 0xA0, 0xF6, 0xCB, 0x71, 0xE1, 0x71, 0xE9, 0x89, 0xE0, 0x13, 0xDD, 0x75, 0x0E, 0x75, 0x75, 0x27, 0x35, 0x4E, 0x16, 0xD5, 0xC3, 0xF5, 0x92, 0xFA, 0x89, 0x53, 0xD1, 0xA7, 0x6E, 0xFF, 0xE4, 0xF5, 0x53, 0x6B, 0x03, 0xA3, 0xA1, 0xAA, 0x51, 0xA5, 0xB1, 0xE0, 0x34, 0x38, 0x2D, 0x39, 0xFD, 0xE2, 0xE7, 0x98, 0x9F, 0x07, 0xCF, 0x04, 0x9C, 0xE9, 0x3A, 0xCB, 0x3C, 0xDB, 0xF0, 0x8B, 0xC1, 0x2F, 0x95, 0x4D, 0xD4, 0xA6, 0xFC, 0x66, 0xA8, 0x79, 0x6D, 0xF3, 0x54,
        0x4B, 0x42, 0x8B, 0xB4, 0x35, 0xB2, 0xB5, 0xEF, 0x9C, 0xFF, 0xB9, 0xAE, 0x36, 0xE7, 0xB6, 0xA6, 0x5F, 0x2D, 0x7E, 0x3D, 0xDE, 0xAE, 0xD3, 0x5E, 0x71, 0x5E, 0xF9, 0x7C, 0x51, 0x07, 0xB1, 0x23, 0xB7, 0x63, 0xF6, 0xC2, 0xBA, 0x0B, 0xD3, 0x9D, 0x69, 0x9D, 0x93, 0x17, 0xE3, 0x2F, 0x8E, 0x76, 0xAD, 0xE9, 0x1A, 0xBE, 0x14, 0x71, 0xE9, 0x6E, 0xF7, 0xF2, 0xEE, 0xDE, 0xCB, 0x01, 0x97, 0xAF, 0x5E, 0xF1, 0xB9, 0x72, 0xA9, 0xC7, 0xA3, 0xE7, 0xC2, 0x55, 0x97, 0xAB, 0xED, 0xD7, 0x9C, 0xAE, 0x9D, 0xBB, 0xCE, 0xBC, 0xDE, 0x72, 0xC3, 0xFE, 0x46, 0xF3, 0x4D, 0xBB, 0x9B, 0x4D, 0xBF, 0xD9, 0xFD, 0xD6, 0xD4, 0x6B, 0xDF, 0xDB, 0x7C, 0xCB, 0xE1, 0x56, 0xEB, 0x6D,
        0xC7, 0xDB, 0x6D, 0x7D, 0x8B, 0xFB, 0x3A, 0xFA, 0xDD, 0xFA, 0x2F, 0xDE, 0xF1, 0xBA, 0x73, 0xE5, 0x2E, 0xFB, 0xEE, 0x8D, 0x81, 0x65, 0x03, 0x7D, 0x83, 0x2B, 0x06, 0xEF, 0x0D, 0x45, 0x0F, 0x49, 0xEF, 0xF1, 0xEE, 0x8D, 0xDF, 0x4F, 0xBE, 0xFF, 0xFA, 0x41, 0xE6, 0x83, 0x99, 0xE1, 0x2D, 0x0F, 0x31, 0x0F, 0xF3, 0x1F, 0x29, 0x3C, 0x2A, 0x7D, 0xAC, 0xF1, 0xB8, 0xFA, 0x89, 0xC9, 0x93, 0x46, 0xA9, 0xBD, 0xF4, 0xFC, 0x88, 0xD7, 0xC8, 0xCD, 0xA7, 0x21, 0x4F, 0x87, 0x47, 0xB9, 0xA3, 0x2F, 0x7F, 0xCF, 0xF8, 0xFD, 0xF3, 0x58, 0xEE, 0x33, 0xCA, 0xB3, 0xD2, 0xE7, 0xDA, 0xCF, 0xEB, 0xC6, 0x6D, 0xC6, 0xDB, 0x27, 0x7C, 0x26, 0x6E, 0xBF, 0x58, 0xF9, 0x62, 0xEC,
        0x65, 0xDA, 0xCB, 0x99, 0xC9, 0xBC, 0x3F, 0x14, 0xFF, 0xA8, 0x7C, 0x65, 0xFC, 0xEA, 0x97, 0x3F, 0xDD, 0xFF, 0xBC, 0x39, 0x15, 0x31, 0x35, 0xF6, 0x5A, 0xF4, 0x7A, 0xF6, 0xAF, 0xC2, 0x37, 0x6A, 0x6F, 0x8E, 0xBF, 0xB5, 0x7D, 0xDB, 0x35, 0x1D, 0x34, 0xFD, 0xF8, 0x5D, 0xCA, 0xBB, 0x99, 0xF7, 0xF9, 0x1F, 0xD4, 0x3E, 0x9C, 0xF8, 0xC8, 0xFC, 0xD8, 0xF3, 0x29, 0xFC, 0xD3, 0xF3, 0x99, 0xAC, 0xCF, 0xB8, 0xCF, 0x65, 0x5F, 0x4C, 0xBE, 0xB4, 0x7D, 0x0D, 0xF8, 0xFA, 0x70, 0x36, 0x65, 0x76, 0xF6, 0x3B, 0x0F, 0x62, 0xC1, 0xF6, 0x66, 0xD1, 0xED, 0xAD, 0x1D, 0xED, 0xED, 0x19, 0x36, 0x0C, 0x6B, 0xBA, 0x17, 0x3F, 0x8E, 0x23, 0x49, 0x16, 0xD3, 0x65, 0xCE, 0x8E,
        0x95, 0x9A, 0x9C, 0x2A, 0x11, 0xD1, 0x43, 0xD2, 0x38, 0x5C, 0x3E, 0x9D, 0x41, 0x97, 0x99, 0x95, 0xFF, 0x9B, 0x4F, 0x89, 0x3D, 0x08, 0x40, 0xCB, 0x76, 0x00, 0x54, 0x1F, 0x7C, 0x8B, 0x21, 0x0A, 0x9A, 0x7F, 0xCC, 0x7B, 0xB2, 0x39, 0x41, 0xE0, 0x9F, 0x05, 0x7F, 0x57, 0x77, 0x18, 0xB9, 0x64, 0x5E, 0x6B, 0xC7, 0xB7, 0x58, 0x6A, 0x35, 0x00, 0xCC, 0x69, 0x00, 0x50, 0x7B, 0x32, 0x04, 0xF1, 0x73, 0x31, 0x56, 0x70, 0x28, 0xFD, 0xBB, 0x39, 0x30, 0x82, 0xF9, 0x71, 0x7C, 0x11, 0x5F, 0x88, 0x6C, 0x35, 0x4C, 0xC0, 0xCF, 0x12, 0x08, 0xE3, 0x91, 0xFD, 0x0B, 0x79, 0x02, 0xB1, 0x20, 0x55, 0x48, 0x17, 0x08, 0xE9, 0x7F, 0x1B, 0xD3, 0xFF, 0xBE, 0xF3, 0xBF, 0xEB,
        0x5B, 0x9F, 0xDF, 0xBC, 0xB1, 0x98, 0x9F, 0x2D, 0x9E, 0xEB, 0x33, 0x35, 0x6D, 0xAD, 0x48, 0x10, 0x9F, 0x20, 0xA6, 0xB3, 0x85, 0x62, 0xBE, 0x48, 0xC8, 0x91, 0x75, 0xC4, 0x49, 0x9E, 0x3B, 0x1D, 0x91, 0xAC, 0xC7, 0x8C, 0x54, 0x91, 0x58, 0x20, 0x49, 0x31, 0xA7, 0x23, 0x4E, 0xD2, 0x0E, 0x80, 0x8C, 0xB8, 0x45, 0x36, 0x73, 0x4B, 0x41, 0x64, 0xC4, 0x23, 0x63, 0x9E, 0xCC, 0xCE, 0xBE, 0x31, 0x04, 0x00, 0x57, 0x02, 0xC0, 0x97, 0xA2, 0xD9, 0xD9, 0x99, 0xAA, 0xD9, 0xD9, 0x2F, 0xC8, 0x2C, 0x50, 0xC3, 0x00, 0x74, 0x4A, 0xFE, 0x05, 0x8D, 0x08, 0xD1, 0x23 };
}