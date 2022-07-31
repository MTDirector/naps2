using NAPS2.Images.Gdi;
using NAPS2.ImportExport.Pdf;
using NAPS2.Ocr;
using NAPS2.Sdk.Tests.Asserts;
using Xunit;

namespace NAPS2.Sdk.Tests.ImportExport.Pdf;

public class PdfImportExportTests : ContextualTests
{
    private readonly PdfSharpImporter _importer;
    private readonly PdfSharpExporter _exporter;
    private readonly string _importPath;
    private readonly string _exportPath;

    public PdfImportExportTests()
    {
        _importer = new PdfSharpImporter(ScanningContext);
        _exporter = new PdfSharpExporter(ScanningContext);
        _importPath = CopyResourceToFile(PdfResources.word_generated_pdf, "import.pdf");
        _exportPath = Path.Combine(FolderPath, "export.pdf");
    }

    [Theory]
    [ClassData(typeof(StorageAwareTestData))]
    public async Task ImportInsertExport(StorageConfig storageConfig)
    {
        storageConfig.Apply(this);

        var images = await _importer.Import(_importPath).ToList();
        Assert.Equal(2, images.Count);

        var toInsert = ScanningContext.CreateProcessedImage(new GdiImage(ImageResources.color_image));
        var newImages = new List<ProcessedImage>
        {
            images[0],
            toInsert,
            images[1]
        };
        await _exporter.Export(_exportPath, newImages, new PdfExportParams());

        PdfAsserts.AssertImages(_exportPath, PdfResources.word_p1, ImageResources.color_image, PdfResources.word_p2);
    }

    [Theory]
    [ClassData(typeof(StorageAwareTestData))]
    public async Task ImportTransformExport(StorageConfig storageConfig)
    {
        storageConfig.Apply(this);

        var images = await _importer.Import(_importPath).ToList();
        Assert.Equal(2, images.Count);

        var newImages = new List<ProcessedImage>
        {
            images[0].WithTransform(new RotationTransform(90)),
            images[1].WithTransform(new BlackWhiteTransform())
        };
        ImageAsserts.Similar(PdfResources.word_p1_rotated, ImageContext.Render(newImages[0]));
        ImageAsserts.Similar(PdfResources.word_p2_bw, ImageContext.Render(newImages[1]));

        await _exporter.Export(_exportPath, newImages, new PdfExportParams());
        PdfAsserts.AssertImages(_exportPath, PdfResources.word_p1_rotated, PdfResources.word_p2_bw);
    }

    [Theory]
    [ClassData(typeof(StorageAwareTestData))]
    public async Task ImportExportWithOcr(StorageConfig storageConfig)
    {
        storageConfig.Apply(this);
        SetUpOcr();

        var importPathForOcr = Path.Combine(FolderPath, "import_ocr.pdf");
        File.WriteAllBytes(importPathForOcr, PdfResources.word_patcht_pdf);
        var images = await _importer.Import(_importPath).ToList();
        var imagesForOcr = await _importer.Import(importPathForOcr).ToList();

        Assert.Equal(2, images.Count);
        Assert.Single(imagesForOcr);
        ImageAsserts.Similar(PdfResources.word_patcht_p1, ImageContext.Render(imagesForOcr[0]));

        var allImages = images.Concat(imagesForOcr).ToList();

        await _exporter.Export(_exportPath, allImages, new PdfExportParams(), new OcrParams("eng", OcrMode.Fast, 0));
        PdfAsserts.AssertImages(_exportPath, PdfResources.word_p1, PdfResources.word_p2, PdfResources.word_patcht_p1);
        PdfAsserts.AssertContainsTextOnce("Page one.", _exportPath);
        PdfAsserts.AssertContainsTextOnce("Sized for printing unscaled", _exportPath);
    }
}