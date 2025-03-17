using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Pdf2Image.Exceptions;
using Pdf2Image.Parsers;

namespace Pdf2Image;

/// <summary>
/// Defines a PDF to image converter.
/// </summary>
public static class Pdf2ImageConverter
{
    private static readonly List<PdfConverterOptionsFormat> s_transparentFileTypes = [PdfConverterOptionsFormat.PNG, PdfConverterOptionsFormat.TIFF];
    private static readonly List<string> s_pdfInfoConvertToInt = ["Pages"];

    /// <summary>
    /// Converts a PDF file from the specified bytes to a list of images.
    /// </summary>
    /// <param name="pdfFile">The PDF file as a byte array.</param>
    /// <param name="options">The optional PDF conversion configuration options.</param>
    /// <returns>A list of images as byte arrays representing the pages of the PDF file.</returns>
    /// <exception cref="PdfConvertException">Thrown when an error occurs during the PDF conversion process.</exception>
    /// <exception cref="PdfInfoException">Thrown when the PDF information cannot be retrieved.</exception>
    /// <exception cref="NotSupportedException">Thrown when the image format is not supported.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the start page is less than 1 or greater than the total number of pages.</exception>
    public static async Task<List<byte[]>> FromBytesAsync(
        byte[] pdfFile,
        PdfConverterOptions? options = null)
    {
        options ??= new PdfConverterOptions();

        var tempFileName = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFileName, pdfFile);
            return await FromPathAsync(tempFileName, options);
        }
        finally
        {
            if (File.Exists(tempFileName))
            {
                File.Delete(tempFileName);
            }
        }
    }

    /// <summary>
    /// Converts a PDF file from the specified path to a list of images.
    /// </summary>
    /// <param name="pdfPath">The path to the PDF file.</param>
    /// <param name="options">The optional PDF conversion configuration options.</param>
    /// <returns>A list of images as byte arrays representing the pages of the PDF file.</returns>
    /// <exception cref="PdfInfoException">Thrown when the PDF information cannot be retrieved.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the start page is less than 1 or greater than the total number of pages.</exception>
    /// <exception cref="NotSupportedException">Thrown when the image format is not supported.</exception>
    /// <exception cref="PdfConvertException">Thrown when an error occurs during the PDF conversion process.</exception>
    public static async Task<List<byte[]>> FromPathAsync(
        string pdfPath,
        PdfConverterOptions? options = null)
    {
        options ??= new PdfConverterOptions();

        // Gets the PDF information to determine the number of pages.
        var info = PdfInfoFromPath(pdfPath, options);
        var pageCount = (int)info["Pages"];

        // Ensure the concurrency is at least 1.
        var concurrency = options.Concurrency;
        if (concurrency < 1)
        {
            concurrency = 1;
        }

        // Determine the start and end pages to process.
        var startPage = options.FirstPage ?? 1;
        var endPage = options.LastPage ?? pageCount;
        if (startPage < 1 || startPage > endPage)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                $"The first page must be between 1 and {endPage}.");
        }

        var totalPages = endPage - startPage + 1;
        if (concurrency > totalPages)
        {
            concurrency = totalPages;
        }

        // Determine the format and parsing function to use.
        Func<byte[], List<byte[]>>? parseBufferFunc = options.Format switch
        {
            PdfConverterOptionsFormat.PNG => PngParser.ParseBytes,
            PdfConverterOptionsFormat.JPEG => JpegParser.ParseBytes,
            PdfConverterOptionsFormat.TIFF => null,
            _ => throw new NotSupportedException($"The image format {options.Format} is not supported.")
        };

        var finalExtension = options.Format switch
        {
            PdfConverterOptionsFormat.PNG => "png",
            PdfConverterOptionsFormat.JPEG => "jpg",
            PdfConverterOptionsFormat.TIFF => "tif",
            _ => throw new NotSupportedException($"The image format {options.Format} is not supported.")
        };

        var usePdfCairo = s_transparentFileTypes.Contains(options.Format);

        // Create a temporary folder if needed.
        var autoTempDir = false;
        var tempFolder = options.OutputFolder;
        if (usePdfCairo && string.IsNullOrEmpty(tempFolder))
        {
            tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempFolder);
            autoTempDir = true;
        }

        // Determine the page ranges to process.
        var pageRanges = new List<(int first, int last)>();
        var remainder = totalPages % concurrency;
        var currentPage = startPage;
        for (var i = 0; i < concurrency; i++)
        {
            var chunkSize = (totalPages / concurrency) + (remainder > 0 ? 1 : 0);
            remainder -= remainder > 0 ? 1 : 0;

            var chunkFirst = currentPage;
            var chunkLast = currentPage + chunkSize - 1;
            currentPage += chunkSize;

            pageRanges.Add((chunkFirst, chunkLast));
        }

        // Build the process information for each page range.
        List<string> outputNames = [];
        for (var i = 0; i < concurrency; i++)
        {
            outputNames.Add($"output-{i:D4}");
        }

        var tasks = new List<Task<(string uid, byte[] stoutData, string stderr)>>();
        for (var i = 0; i < concurrency; i++)
        {
            var (localFirst, localLast) = pageRanges[Math.Min(i, pageRanges.Count - 1)];
            var outName = outputNames[Math.Min(i, outputNames.Count - 1)];

            var (uid, psi) = BuildPopplerProcessInfo(
                pdfPath,
                options.Dpi,
                tempFolder,
                localFirst,
                localLast,
                options.Format,
                outName,
                options.UserPassword,
                options.OwnerPassword,
                options.UseCropbox,
                options.HideAnnotations,
                options.Transparent,
                options.Grayscale,
                (options.Width, options.Height),
                usePdfCairo,
                options.PopplerPath);

            tasks.Add(RunPopplerAsync(uid, psi));
        }

        var results = await Task.WhenAll(tasks);

        // Gather the final images
        List<byte[]> allImages = [];
        foreach (var (uid, stdoutData, stderr) in results)
        {
            if (!string.IsNullOrEmpty(stderr))
            {
                Debug.WriteLine($"Error converting PDF to images: {stderr}");

                if (!stderr.Contains("Syntax Error"))
                {
                    // If the error is not a syntax error, throw an exception.
                    throw new PdfConvertException($"Error converting PDF to images: {stderr}");
                }
            }


            if (!string.IsNullOrEmpty(tempFolder))
            {
                var imagesFromFolder = LoadFromOutputFolder(tempFolder, uid, finalExtension);
                allImages.AddRange(imagesFromFolder);
            }
            else
            {
                if (parseBufferFunc == null)
                {
                    Debug.WriteLine($"No parser function for format {options.Format}");
                    continue;
                }

                var imagesFromBuffer = parseBufferFunc(stdoutData);
                allImages.AddRange(imagesFromBuffer);
            }
        }

        if (!autoTempDir || !Directory.Exists(tempFolder))
        {
            return allImages;
        }

        try
        {
            Directory.Delete(tempFolder, true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting temporary folder: {ex.Message}");
        }

        return allImages;
    }

    private static List<byte[]> LoadFromOutputFolder(
        string outputFolder,
        string outputFile,
        string extension)
    {
        List<byte[]> results = [];
        var files = Directory.GetFiles(outputFolder).Select(Path.GetFileName).ToList();
        files.Sort();

        foreach (var fullPath in from file in files
                                 where file.StartsWith(outputFile) && file.EndsWith($".{extension}")
                                 select Path.Combine(outputFolder, file))
        {
            try
            {
                var bytes = File.ReadAllBytes(fullPath);
                results.Add(bytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading image from {fullPath}: {ex.Message}");
            }
        }

        return results;
    }

    private static async Task<(string uid, byte[] stdoutData, string stderr)> RunPopplerAsync(
        string uid,
        ProcessStartInfo psi)
    {
        using var proc = new Process();
        proc.StartInfo = psi;

        try
        {
            proc.Start();
        }
        catch (Exception ex)
        {
            throw new PopplerNotInstalledException("Is poppler installed or in the PATH? If not, set the PopplerPath configuration option.", ex);
        }

        var stdoutMemory = new MemoryStream();
        var copyStdoutTask = proc.StandardOutput.BaseStream.CopyToAsync(stdoutMemory);
        var stdErrTask = proc.StandardError.ReadToEndAsync();

        await proc.WaitForExitAsync();

        await copyStdoutTask;
        var stderr = await stdErrTask;

        var stdoutData = stdoutMemory.ToArray();
        return (uid, stdoutData, stderr);
    }

    private static (string uid, ProcessStartInfo psi) BuildPopplerProcessInfo(
        string pdfPath,
        int dpi,
        string? outputFolder,
        int firstPage,
        int lastPage,
        PdfConverterOptionsFormat format,
        string outputFile,
        string? userPassword,
        string? ownerPassword,
        bool useCropbox,
        bool hideAnnotations,
        bool transparent,
        bool grayscale,
        (int? Width, int? Height) size,
        bool usePdfCairo,
        string? popplerPath)
    {
        var cmdArgs = GetCommandArgs();
        var executableName = usePdfCairo ? "pdftocairo" : "pdftoppm";
        var executablePath = GetCommandPath(executableName, popplerPath);

        var psi = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = string.Join(" ", cmdArgs),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (string.IsNullOrEmpty(popplerPath))
        {
            return (outputFile, psi);
        }

        var pathEnv = psi.EnvironmentVariables.ContainsKey("PATH")
            ? psi.EnvironmentVariables["PATH"]
            : string.Empty;
        pathEnv = popplerPath + Path.PathSeparator + pathEnv;
        psi.EnvironmentVariables["PATH"] = pathEnv;

        return (outputFile, psi);

        List<string> GetCommandArgs()
        {
            List<string> args = [];
            args.AddRange(["-r", dpi.ToString(CultureInfo.InvariantCulture), pdfPath]);

            if (useCropbox)
            {
                args.Add("-cropbox");
            }

            if (hideAnnotations)
            {
                args.Add("-hide-annotations");
            }

            if (transparent && s_transparentFileTypes.Contains(format))
            {
                args.Add("-transp");
            }

            if (firstPage > 0)
            {
                args.AddRange(["-f", firstPage.ToString(CultureInfo.InvariantCulture)]);
            }

            if (lastPage > 0)
            {
                args.AddRange(["-l", lastPage.ToString(CultureInfo.InvariantCulture)]);
            }

            var formatArg = format switch
            {
                PdfConverterOptionsFormat.PNG => "png",
                PdfConverterOptionsFormat.JPEG => "jpeg",
                PdfConverterOptionsFormat.TIFF => "tiff",
                _ => throw new NotSupportedException($"The image format {format} is not supported.")
            };

            args.AddRange([$"-{formatArg}"]);

            if (!string.IsNullOrEmpty(outputFolder))
            {
                var outPath = Path.Combine(outputFolder, outputFile);
                args.Add(outPath);
            }

            if (!string.IsNullOrEmpty(userPassword))
            {
                args.AddRange(["-upw", userPassword]);
            }

            if (!string.IsNullOrEmpty(ownerPassword))
            {
                args.AddRange(["-opw", ownerPassword]);
            }

            if (grayscale)
            {
                args.Add("-gray");
            }

            var w = size.Width;
            var h = size.Height;

            if (w != null && h != null)
            {
                args.AddRange(["-scale-to-x", w.Value.ToString(CultureInfo.InvariantCulture), "-scale-to-y", h.Value.ToString(CultureInfo.InvariantCulture)]);
            }
            else if (w != null)
            {
                args.AddRange(["-scale-to-x", w.Value.ToString(CultureInfo.InvariantCulture), "-scale-to-y", "-1"]);
            }
            else if (h != null)
            {
                args.AddRange(["-scale-to-x", "-1", "-scale-to-y", h.Value.ToString(CultureInfo.InvariantCulture)]);
            }

            return args;
        }
    }

    /// <exception cref="PopplerNotInstalledException">Thrown when the Poppler binaries are not installed or in the PATH.</exception>
    /// <exception cref="PdfInfoException">Thrown when the PDF information cannot be retrieved.</exception>
    private static Dictionary<string, object> PdfInfoFromPath(
        string pdfPath,
        PdfOptions options)
    {
        var executablePath = GetCommandPath("pdfinfo", options.PopplerPath);
        var args = new List<string> { pdfPath };

        if (!string.IsNullOrEmpty(options.UserPassword))
        {
            args.AddRange(["-upw", options.UserPassword]);
        }

        if (!string.IsNullOrEmpty(options.OwnerPassword))
        {
            args.AddRange(["-opw", options.OwnerPassword]);
        }

        if (options.FirstPage != null)
        {
            args.AddRange(["-f", options.FirstPage.Value.ToString(CultureInfo.InvariantCulture)]);
        }

        if (options.LastPage != null)
        {
            args.AddRange(["-l", options.LastPage.Value.ToString(CultureInfo.InvariantCulture)]);
        }

        var psi = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (!string.IsNullOrEmpty(options.PopplerPath))
        {
            var pathEnv = psi.EnvironmentVariables.ContainsKey("PATH")
                ? psi.EnvironmentVariables["PATH"]
                : string.Empty;
            pathEnv = options.PopplerPath + Path.PathSeparator + pathEnv;
            psi.EnvironmentVariables["PATH"] = pathEnv;
        }

        using var proc = new Process();
        proc.StartInfo = psi;

        try
        {
            proc.Start();
        }
        catch (Exception ex)
        {
            throw new PopplerNotInstalledException("Is poppler installed or in the PATH? If not, set the PopplerPath configuration option.", ex);
        }

        proc.WaitForExit();

        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();

        var result = new Dictionary<string, object>();

        var lines = stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (!line.Contains(':'))
            {
                continue;
            }

            var idx = line.IndexOf(':');
            if (idx < 0)
            {
                continue;
            }

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();
            result[key] = s_pdfInfoConvertToInt.Contains(key)
                ? int.TryParse(value, out var intValue) ? intValue : value
                : value;
        }

        if (!result.ContainsKey("Pages"))
        {
            throw new PdfInfoException($"Unable to get page count from {pdfPath}. {stderr}");
        }

        return result;
    }

    private static string GetCommandPath(string command, string? popplerPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !command.EndsWith(".exe"))
        {
            command += ".exe";
        }

        return !string.IsNullOrEmpty(popplerPath) ? Path.Combine(popplerPath, command) : command;
    }
}
