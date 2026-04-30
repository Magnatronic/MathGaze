using MathGaze.ViewModels;
using System.Windows;

namespace MathGaze;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
