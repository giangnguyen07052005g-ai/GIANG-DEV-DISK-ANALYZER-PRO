using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.WindowsIntegration;
using Path = System.IO.Path;

namespace GIANG.Dev.DiskAnalyzer.App.ViewModels;

public partial class FolderAnalyzerViewModel : ObservableObject
{
    private readonly IFolderAnalyzer _analyzer;
    private readonly IFeatureService _featureService;
    private CancellationTokenSource? _cts;
    private readonly Stack<FolderItem> _navigationHistory = new();

    [ObservableProperty]
    private string _targetPath = @"C:\";

    [ObservableProperty]
    private bool _isAnalyzing;

    [ObservableProperty]
    private string _statusMessage = "Sẵn sàng phân tích thư mục";

    [ObservableProperty]
    private FolderItem? _currentFolder;

    [ObservableProperty]
    private ObservableCollection<FolderItem> _subFolders = new();

    [ObservableProperty]
    private FolderItem? _selectedSubFolder;

    [ObservableProperty]
    private ObservableCollection<TreemapNode> _treemapNodes = new();

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private string _currentBreadcrumb = string.Empty;

    public FolderAnalyzerViewModel(IFolderAnalyzer analyzer, IFeatureService featureService)
    {
        _analyzer = analyzer;
        _featureService = featureService;
    }

    [RelayCommand]
    public void BrowseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Chọn thư mục phân tích đa cấp",
            InitialDirectory = TargetPath
        };

        if (dialog.ShowDialog() == true)
        {
            TargetPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    public async Task StartAnalysisAsync(string? specificPath = null)
    {
        string path = specificPath ?? TargetPath;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            StatusMessage = "Vui lòng chọn đường dẫn thư mục hợp lệ!";
            return;
        }

        IsAnalyzing = true;
        _cts = new CancellationTokenSource();
        _navigationHistory.Clear();
        CanGoBack = false;

        var progress = new Progress<ScanProgress>(p =>
        {
            StatusMessage = p.StatusMessage;
        });

        try
        {
            int maxDepth = _featureService.GetLimit("MaxFolderDepth");
            var root = await _analyzer.AnalyzeFolderAsync(path, maxDepth, progress, _cts.Token);
            SetCurrentFolder(root);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Đã hủy phân tích.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    private void SetCurrentFolder(FolderItem folder)
    {
        CurrentFolder = folder;
        CurrentBreadcrumb = folder.FullPath;
        SubFolders.Clear();
        foreach (var child in folder.Children)
        {
            SubFolders.Add(child);
        }

        // Render treemap nodes (assume 800x400 standard canvas space)
        var nodes = _analyzer.BuildTreemapHierarchy(folder, 800, 360);
        TreemapNodes.Clear();
        foreach (var n in nodes)
        {
            TreemapNodes.Add(n);
        }
    }

    [RelayCommand]
    public void DrillDown(FolderItem? folder)
    {
        if (folder == null || CurrentFolder == null) return;
        _navigationHistory.Push(CurrentFolder);
        CanGoBack = true;
        SetCurrentFolder(folder);
    }

    [RelayCommand]
    public void TreemapNodeClicked(TreemapNode? node)
    {
        if (node == null) return;
        var matchingChild = CurrentFolder?.Children.FirstOrDefault(c => c.FullPath == node.FullPath);
        if (matchingChild != null)
        {
            DrillDown(matchingChild);
        }
    }

    [RelayCommand]
    public void GoBack()
    {
        if (_navigationHistory.Count > 0)
        {
            var prev = _navigationHistory.Pop();
            CanGoBack = _navigationHistory.Count > 0;
            SetCurrentFolder(prev);
        }
    }

    [RelayCommand]
    public void OpenInExplorer(FolderItem? folder)
    {
        if (folder != null) ShellHelper.OpenInExplorer(folder.FullPath);
    }

    [RelayCommand]
    public void CancelAnalysis()
    {
        _cts?.Cancel();
    }
}
