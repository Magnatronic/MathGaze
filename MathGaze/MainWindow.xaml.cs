using MathGaze.ViewModels;
using System.Windows;

namespace MathGaze;

public partial class MainWindow : Window
{
    private readonly PdfCanvasViewModel _pdfCanvasVm;

    public MainWindow(MainViewModel viewModel, PdfCanvasViewModel pdfCanvasViewModel, ToolViewModel toolViewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _pdfCanvasVm = pdfCanvasViewModel;
        // Wire PdfCanvas DataContext to its dedicated ViewModel
        PdfCanvasView.DataContext = pdfCanvasViewModel;
        // Wire ToolRail DataContext to ToolViewModel so command bindings resolve
        ToolRailControl.DataContext = toolViewModel;
    }
}
