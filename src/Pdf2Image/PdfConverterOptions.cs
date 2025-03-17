namespace Pdf2Image;

/// <summary>
/// Defines configuration options for processing PDF files.
/// </summary>
public class PdfOptions
{
    /// <summary>
    /// Gets or sets the first page in a range of pages to process.
    /// </summary>
    /// <remarks>
    /// If this value is <c>null</c>, the first page in the PDF file will be used.
    /// </remarks>
    public int? FirstPage { get; set; }

    /// <summary>
    /// Gets or sets the last page in a range of pages to process.
    /// </summary>
    /// <remarks>
    /// If this value is <c>null</c>, the last page in the PDF file will be used.
    /// </remarks>
    public int? LastPage { get; set; }

    /// <summary>
    /// Gets or sets the user password for the PDF file.
    /// </summary>
    public string? UserPassword { get; set; }

    /// <summary>
    /// Gets or sets the owner password for the PDF file.
    /// </summary>
    public string? OwnerPassword { get; set; }

    /// <summary>
    /// Gets or sets the path to the Poppler binaries, if not in the system PATH.
    /// </summary>
    /// <remarks>
    /// In Windows, if this values in not in the system PATH, it should be the path to the directory containing the Poppler binaries, e.g., <c>C:\path\to\poppler-xx\bin</c>.
    /// </remarks>
    public string? PopplerPath { get; set; }
}

/// <summary>
/// Defines configuration options for converting PDF files to images.
/// </summary>
public class PdfConverterOptions : PdfOptions
{
    /// <summary>
    /// Gets or sets the DPI (dots per inch) for the output images. Default is <c>200</c>.
    /// </summary>
    public int Dpi { get; set; } = 200;

    /// <summary>
    /// Gets or sets the format of the output images. Default is <see cref="PdfConverterOptionsFormat.PNG"/>.
    /// </summary>
    public PdfConverterOptionsFormat Format { get; set; } = PdfConverterOptionsFormat.PNG;

    /// <summary>
    /// Gets or sets the number of concurrent tasks to use for processing the PDF file. Default is <c>1</c>.
    /// </summary>
    public int Concurrency { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether to use cropbox instead of mediabox. Default is <c>false</c>.
    /// </summary>
    public bool UseCropbox { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use a transparent background for the images instead of white. Default is <c>false</c>.
    /// </summary>
    public bool Transparent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to convert the images to grayscale. Default is <c>false</c>.
    /// </summary>
    public bool Grayscale { get; set; }

    /// <summary>
    /// Gets or sets the desired width for the output images. Default is <c>null</c>.
    /// </summary>
    /// <remarks>
    /// If this value is <c>null</c>, the width will be calculated based on the aspect ratio of the PDF file in relation to the height.
    /// </remarks>
    public int? Width { get; set; }

    /// <summary>
    /// Gets or sets the desired height for the output images. Default is <c>null</c>.
    /// </summary>
    /// <remarks>
    /// If this value is <c>null</c>, the height will be calculated based on the aspect ratio of the PDF file in relation to the width.
    /// </remarks>
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to hide annotations in the PDF file. Default is <c>false</c>.
    /// </summary>
    public bool HideAnnotations { get; set; }

    /// <summary>
    /// Gets or sets the output folder for the images.
    /// </summary>
    /// <remarks>
    /// If this value is <c>null</c>, the images will be saved in a temporary folder.
    /// </remarks>
    public string? OutputFolder { get; set; }
}

/// <summary>
/// Defines the formats for the output images.
/// </summary>
public enum PdfConverterOptionsFormat
{
    /// <summary>
    /// Portable Network Graphics (PNG) format.
    /// </summary>
    PNG = 0,

    /// <summary>
    /// Joint Photographic Experts Group (JPEG) format.
    /// </summary>
    JPEG = 1,

    /// <summary>
    /// Tagged Image File Format (TIFF) format.
    /// </summary>
    TIFF = 2,
}
