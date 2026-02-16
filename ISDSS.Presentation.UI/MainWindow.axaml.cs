using Avalonia.Controls;
using ISDSS.Presentation.UI.ViewModels;

namespace ISDSS.Presentation.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
