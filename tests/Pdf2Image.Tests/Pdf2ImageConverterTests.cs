using System.Runtime.InteropServices;
using Pdf2Image.Exceptions;

namespace Pdf2Image.Tests;

[TestFixture]
public class Pdf2ImageConverterTests
{
    // Paths to test files
    private static readonly string TestPdfPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "Test.pdf");
    private static readonly string CorruptedPdfPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "Test_Corrupted.pdf");
    private static readonly string LockedPdfPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "Test_Locked.pdf");
    private const string LockedPdfPassword = "1984Bbiwy!";

    private string? popplerPath = @"";


    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        if (!IsPopplerInstalled())
        {
            Assert.Ignore("Poppler utilities not detected. Skipping tests.");
        }
    }

    private bool IsPopplerInstalled()
    {
        try
        {
            var executable = "pdfinfo";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                executable += ".exe";
            }

            if (!string.IsNullOrEmpty(popplerPath))
            {
                executable = Path.Combine(popplerPath, executable);
            }

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var proc = new System.Diagnostics.Process();
            proc.StartInfo = processInfo;
            proc.Start();
            proc.WaitForExit();

            return true;
        }
        catch
        {
            return false;
        }
    }

    [Test]
    public async Task FromPathAsync_ValidPdf_ConvertsAllPages()
    {
        // Arrange
        var options = new PdfConverterOptions
        {
            Format = PdfConverterOptionsFormat.PNG,
            Dpi = 100,
            PopplerPath = popplerPath
        };

        // Act
        var images = await Pdf2ImageConverter.FromPathAsync(TestPdfPath, options);

        // Assert
        Assert.That(images, Is.Not.Null.And.Not.Empty);
        Assert.That(images.Count, Is.EqualTo(13));
    }

    [Test]
    public void FromPathAsync_CorruptedPdf_ThrowsPdfInfoException()
    {
        // Arrange
        var options = new PdfConverterOptions
        {
            Format = PdfConverterOptionsFormat.PNG,
            Dpi = 100,
            PopplerPath = popplerPath
        };

        // Act & Assert
        Assert.ThrowsAsync<PdfInfoException>(async () =>
        {
            await Pdf2ImageConverter.FromPathAsync(CorruptedPdfPath, options);
        });
    }

    [Test]
    public async Task FromPathAsync_LockedPdfWithPassword_ConvertsSuccessfully()
    {
        // Arrange
        var options = new PdfConverterOptions
        {
            Format = PdfConverterOptionsFormat.PNG,
            Dpi = 100,
            UserPassword = LockedPdfPassword,
            PopplerPath = popplerPath
        };

        // Act
        var images = await Pdf2ImageConverter.FromPathAsync(LockedPdfPath, options);

        // Assert
        Assert.That(images, Is.Not.Null.And.Not.Empty);
        Assert.That(images.Count, Is.EqualTo(13));
    }

    [Test]
    public void FromPathAsync_LockedPdfWithoutPassword_ThrowsPdfInfoException()
    {
        // Arrange
        var options = new PdfConverterOptions
        {
            Format = PdfConverterOptionsFormat.PNG,
            Dpi = 100,
            PopplerPath = popplerPath
        };

        // Act & Assert
        Assert.ThrowsAsync<PdfInfoException>(async () =>
        {
            await Pdf2ImageConverter.FromPathAsync(LockedPdfPath, options);
        });
    }

    [Test]
    public async Task FromBytesAsync_ValidPdfBytes_ConvertsCorrectly()
    {
        // Arrange
        var pdfBytes = await File.ReadAllBytesAsync(TestPdfPath);
        var options = new PdfConverterOptions
        {
            Format = PdfConverterOptionsFormat.JPEG,
            Dpi = 72,
            PopplerPath = popplerPath
        };

        // Act
        var images = await Pdf2ImageConverter.FromBytesAsync(pdfBytes, options);

        // Assert
        Assert.That(images, Is.Not.Null.And.Not.Empty);
        Assert.That(images.Count, Is.EqualTo(13));
    }

    [Test]
    public void FromPathAsync_UnsupportedFormat_ThrowsNotSupportedException()
    {
        // Arrange
        var options = new PdfConverterOptions
        {
            // Casting an out-of-range number to PdfConverterOptionsFormat
            Format = (PdfConverterOptionsFormat)999,
            Dpi = 100,
            PopplerPath = popplerPath
        };

        // Act & Assert
        Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await Pdf2ImageConverter.FromPathAsync(TestPdfPath, options);
        });
    }

    [Test]
    public async Task FromPathAsync_PartialPageRange_ConvertsOnlySpecifiedPages()
    {
        // Arrange
        var options = new PdfConverterOptions
        {
            Format = PdfConverterOptionsFormat.PNG,
            Dpi = 100,
            FirstPage = 3,
            LastPage = 5,
            PopplerPath = popplerPath
        };

        // Act
        var images = await Pdf2ImageConverter.FromPathAsync(TestPdfPath, options);

        // Assert
        Assert.That(images, Is.Not.Null.And.Not.Empty);
        Assert.That(images.Count, Is.EqualTo(3));
    }

    [Test]
    public void FromPathAsync_InvalidPageRange_ThrowsPdfInfoException()
    {
        // Arrange
        var options = new PdfConverterOptions
        {
            Format = PdfConverterOptionsFormat.PNG,
            Dpi = 100,
            FirstPage = 14, // greater than total pages
            LastPage = 13,
            PopplerPath = popplerPath
        };

        // Act & Assert
        Assert.ThrowsAsync<PdfInfoException>(async () =>
        {
            await Pdf2ImageConverter.FromPathAsync(TestPdfPath, options);
        });
    }
}
