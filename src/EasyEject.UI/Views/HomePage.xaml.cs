using EasyEject.UI.Services;
using EasyEject.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EasyEject.UI.Views;

/// <summary>
/// The main page: device scan, eject all and eject selected.
/// </summary>
public sealed partial class HomePage : UserControl
{
    private readonly InfoBarNotificationService _notifications;
    private readonly DialogService _dialogs;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _refreshTimer;

    /// <summary>
    /// Gets the page view model.
    /// </summary>
    public HomeViewModel ViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HomePage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    /// <param name="notifications">The notification service.</param>
    /// <param name="dialogs">The dialog service.</param>
    public HomePage(HomeViewModel viewModel, InfoBarNotificationService notifications, DialogService dialogs)
    {
        InitializeComponent();
        ViewModel = viewModel;
        _notifications = notifications;
        _dialogs = dialogs;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _notifications.Attach(StatusInfoBar);

        // DialogService needs the XamlRoot, which is only available after the
        // element is in the visual tree (i.e. after the window is shown).
        _dialogs.Initialize(XamlRoot);

        _ = ViewModel.InitializeAsync();

        // Automatic refresh every few seconds.
        _refreshTimer = DispatcherQueue.CreateTimer();
        _refreshTimer.Interval = TimeSpan.FromSeconds(5);
        _refreshTimer.Tick += (_, _) => OnAutoRefresh();
        _refreshTimer.Start();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _refreshTimer?.Stop();
        _refreshTimer = null;
    }

    private async void OnAutoRefresh()
    {
        if (ViewModel.IsBusy)
        {
            return;
        }

        await ViewModel.RefreshAsync();
    }

    private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeviceList.SelectedItem is DeviceItemViewModel item)
        {
            ViewModel.Select(item);
        }
    }

    private void DeviceRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DeviceItemViewModel item })
        {
            ViewModel.Select(item);

            // Keep the ListView highlight in sync with the radio selection.
            DeviceList.SelectedItem = item;
        }
    }
}
