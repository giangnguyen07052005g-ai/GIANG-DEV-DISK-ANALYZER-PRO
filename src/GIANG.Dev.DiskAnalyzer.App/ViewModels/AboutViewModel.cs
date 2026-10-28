using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Infrastructure.WindowsIntegration;

namespace GIANG.Dev.DiskAnalyzer.App.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    private readonly IFeatureService _featureService;

    [ObservableProperty]
    private string _appName = "GIANG DEV DISK ANALYZER PRO";

    [ObservableProperty]
    private string _version = "v1.0.0 Pro Edition";

    [ObservableProperty]
    private string _author = "GIANG DEV";

    [ObservableProperty]
    private string _runtimeInfo;

    [ObservableProperty]
    private string _osInfo;

    [ObservableProperty]
    private string _tierStatus;

    [ObservableProperty]
    private string _disclaimer;

    public AboutViewModel(IFeatureService featureService)
    {
        _featureService = featureService;
        _runtimeInfo = $".NET {Environment.Version} (LTS Windows Desktop)";
        _osInfo = $"{Environment.OSVersion} ({Environment.UserName} @ {Environment.MachineName})";
        _tierStatus = $"Hạng phiên bản hiện tại: {_featureService.CurrentTier}";
        _disclaimer = _featureService.TierDisclaimer;
    }

    [RelayCommand]
    public void OpenGithub()
    {
        ShellHelper.OpenFileWithDefaultApp("https://github.com");
    }
}
