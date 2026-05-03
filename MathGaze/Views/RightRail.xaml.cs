using System.Windows.Controls;

namespace MathGaze.Views;

public partial class RightRail : UserControl
{
    public RightRail()
    {
        InitializeComponent();
    }

    // DataContext is set by MainWindow.xaml.cs after DI resolves RightRailViewModel
}
