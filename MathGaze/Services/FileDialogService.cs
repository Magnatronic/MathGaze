using Microsoft.Win32;

namespace MathGaze.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? ShowOpenPdfDialog()
    {
        var dialog = new OpenFileDialog
        {
            Title       = "Open PDF — MathGaze",
            Filter      = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
            FilterIndex = 1,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
