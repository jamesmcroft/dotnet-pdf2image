namespace Pdf2Image.Exceptions;

/// <summary>
/// Defines an exception thrown when the PDF information cannot be retrieved.
/// </summary>
/// <param name="message">The exception message.</param>
public class PdfInfoException(string message) : Exception(message);
