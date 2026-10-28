using System.Windows.Controls;
using System.Windows.Input;
using GIANG.Dev.DiskAnalyzer.App.ViewModels;
using GIANG.Dev.DiskAnalyzer.Core.Models;

namespace GIANG.Dev.DiskAnalyzer.App.Views;

public partial class SafeCleanupView : UserControl
{
    public SafeCleanupView()
    {
        InitializeComponent();
    }

    private void CleanupDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            if (sender is DataGrid dg && dg.SelectedItems.Count > 0)
            {
                var items = dg.SelectedItems.OfType<CleanupItem>().ToList();
                if (items.Count > 0)
                {
                    bool target = !items[0].IsSelected;
                    foreach (var itm in items)
                    {
                        itm.IsSelected = target;
                    }
                    if (DataContext is SafeCleanupViewModel vm)
                    {
                        vm.RecalculateSelectedTotal();
                    }
                    e.Handled = true;
                }
            }
        }
        else if (e.Key == Key.Delete)
        {
            if (sender is DataGrid dg && DataContext is SafeCleanupViewModel vm)
            {
                if (vm.ExecuteSafeCleanupCommand.CanExecute(dg.SelectedItems))
                {
                    vm.ExecuteSafeCleanupCommand.Execute(dg.SelectedItems);
                    e.Handled = true;
                }
            }
        }
    }
}
