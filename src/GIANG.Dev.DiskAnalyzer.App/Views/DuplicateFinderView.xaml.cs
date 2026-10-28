using System.Windows.Controls;
using System.Windows.Input;
using GIANG.Dev.DiskAnalyzer.App.ViewModels;
using GIANG.Dev.DiskAnalyzer.Core.Models;

namespace GIANG.Dev.DiskAnalyzer.App.Views;

public partial class DuplicateFinderView : UserControl
{
    public DuplicateFinderView()
    {
        InitializeComponent();
    }

    private void DuplicatesDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            if (sender is DataGrid dg && dg.SelectedItems.Count > 0)
            {
                var files = dg.SelectedItems.OfType<FileItem>().ToList();
                if (files.Count > 0)
                {
                    bool target = !files[0].IsSelected;
                    foreach (var f in files)
                    {
                        f.IsSelected = target;
                    }
                    e.Handled = true;
                }
            }
        }
        else if (e.Key == Key.Delete)
        {
            if (sender is DataGrid dg && DataContext is DuplicateFinderViewModel vm)
            {
                if (vm.DeleteSelectedDuplicatesCommand.CanExecute(dg.SelectedItems))
                {
                    vm.DeleteSelectedDuplicatesCommand.Execute(dg.SelectedItems);
                    e.Handled = true;
                }
            }
        }
    }
}
