using System.Windows;
using GIANG.Dev.DiskAnalyzer.App.ViewModels;

namespace GIANG.Dev.DiskAnalyzer.App.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
