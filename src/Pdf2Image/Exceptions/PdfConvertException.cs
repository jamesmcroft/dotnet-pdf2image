namespace Pdf2Image.Exceptions;

/// <summary>
/// Defines an exception thrown when an error occurs during PDF conversion.
/// </summary>
/// <param name="message">The exception message.</param>
public class PdfConvertException(string message) : Exception(message);
