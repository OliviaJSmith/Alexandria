using Alexandria.API.DTOs;

namespace Alexandria.API.Services;

/// <summary>
/// Service for OCR operations using Azure Computer Vision.
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Extracts text from a single book image (cover or barcode).
    /// Optimized for detecting ISBN barcodes and cover text.
    /// </summary>
    Task<OcrExtractionResult> ExtractSingleBookAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts text from a bookshelf image containing multiple book spines.
    /// Uses Read API for dense text extraction.
    /// </summary>
    Task<OcrExtractionResult> ExtractBookshelfAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default);
}
