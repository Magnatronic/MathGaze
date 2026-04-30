namespace MathGaze.Services;

/// <summary>
/// Abstraction over the OS file open dialog.
/// Keeps ViewModels testable and decoupled from WPF Window references.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Show the system open-file dialog filtered to PDF files.
    /// Returns the selected file path, or null if the user cancelled.
    /// Must be called on the UI thread.
    /// </summary>
    string? ShowOpenPdfDialog();
}
