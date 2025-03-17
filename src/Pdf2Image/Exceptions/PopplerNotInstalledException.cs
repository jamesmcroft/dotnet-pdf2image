namespace Pdf2Image.Exceptions;

/// <summary>
/// Defines an exception thrown when the Poppler utilities cannot be found.
/// </summary>
/// <param name="message">The exception message.</param>
/// <param name="innerException">The inner exception.</param>
public class PopplerNotInstalledException(string message, Exception? innerException = null) : Exception(message, innerException);
