# pdf2image - .NET

[![GitHub release](https://img.shields.io/github/release/jamesmcroft/dotnet-pdf2image.svg)](https://github.com/jamesmcroft/dotnet-pdf2image/releases)
[![NuGet](https://img.shields.io/nuget/v/pdf2image-dotnet)](https://www.nuget.org/packages/pdf2image-dotnet/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/pdf2image-dotnet)](https://www.nuget.org/packages/pdf2image-dotnet/)
[![Build status](https://github.com/jamesmcroft/dotnet-pdf2image/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/jamesmcroft/dotnet-pdf2image/actions/workflows/ci.yml)

A .NET library that wraps pdftoppm and pdftocairo to convert PDFs to images as byte arrays. This library is heavily inspired by the Python [pdf2image](https://pypi.org/project/pdf2image/) module.

## Install

```bash
dotnet add package pdf2image-dotnet
```

### Windows

Windows users will require the latest poppler binaries installed. [@oschwartz10612/poppler-windows](https://github.com/oschwartz10612/poppler-windows/releases/) is recommended.

You will need to add the `poppler-<version>/Library/bin` directory to your system's PATH environment variable, or set the `PdfConverterOptions.PopplerPath` property to the path when calling `Pdf2ImageConverter.FromBytesAsync` or `Pdf2ImageConverter.FromPathAsync` methods.

### Linux

Most Linux distros ship with `poppler-utils` pre-installed. If not, you can install it using your package manager.

```bash
sudo apt-get install poppler-utils
```

### macOS

macOS users can install `poppler` using Homebrew.

```bash
brew install poppler
```

## Usage

```csharp
using Pdf2Image;

PdfConverterOptions options = new PdfConverterOptions
{
    Format = PdfConverterOptionsFormat.PNG,
    PopplerPath = "/path/to/poppler" // Optional, only required if poppler is not in PATH
}

List<byte[]> images = await Pdf2ImageConverter.FromPathAsync("path/to/file.pdf", options);

// or

List<byte[]> images = await Pdf2ImageConverter.FromBytesAsync(pdfBytes, options);
```

The `PdfConverterOptions` configuration has the following options:

- `Dpi` (int): The DPI to use when converting the PDF to an image. Default is 200.
- `Format` (PdfConverterOptionsFormat): The image format to use when converting the PDF. Default is `PdfConverterOptionsFormat.PNG`. Supported formats are `PdfConverterOptionsFormat.PNG`, `PdfConverterOptionsFormat.JPEG`, and `PdfConverterOptionsFormat.TIFF`.
- `FirstPage` (int?): The first page in a range of pages to process. If not set, the first page will be used.
- `LastPage` (int?): The last page in a range of pages to process. If not set, the last page will be used.
- `UserPassword` (string?): The user password to use when decrypting the PDF. If not set, the PDF will be processed without a password.
- `OwnerPassword` (string?): The owner password to use when decrypting the PDF. If not set, the PDF will be processed without a password.
- `Concurrency` (int): The number of asynchronous tasks to run concurrently when converting the PDF to images. Default is 1.
- `UseCropbox` (bool): Use the crop box instead of the media box when converting the PDF to images. Default is false.
- `Transparent` (bool): Use a transparent background when converting the PDF to images supporting transparency (e.g., png or tiff). Default is false.
- `Grayscale` (bool): Convert the PDF to grayscale images. Default is false.
- `Width` (int?): The desired width of the output image. If not set, the width will be calculated based on the aspect ratio of the PDF page in relation to the height.
- `Height` (int?): The desired height of the output image. If not set, the height will be calculated based on the aspect ratio of the PDF page in relation to the width.
- `HideAnnotations` (bool): Hide annotations when converting the PDF to images. Default is false.
- `PopplerPath` (string?): The path to the poppler binaries. Only required if poppler is not in PATH.
- `OutputFolder` (string?): An optional output folder to save the images to disk. If not set, the images will still be returned as byte arrays.

## Contributing

Contributions, issues, and feature requests are welcome!

Please check the [issues page](https://github.com/jamesmcroft/dotnet-pdf2image/issues) for open issues. You're actively encouraged to jump in and help where you can.

## License

This project is made available under the terms and conditions of the [MIT license](LICENSE).
