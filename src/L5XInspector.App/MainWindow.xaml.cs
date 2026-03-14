using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace L5XInspector.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        RootNav.SelectedItem = RootNav.MenuItems[0];
        ContentFrame.Navigate(typeof(Views.NavigatorPage));
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item)
            return;

        switch (item.Tag?.ToString())
        {
            case "Navigator":
                ContentFrame.Navigate(typeof(Views.NavigatorPage));
                break;
            case "Stations":
                ContentFrame.Navigate(typeof(Views.StationsPage));
                break;
            case "Trace":
                ContentFrame.Navigate(typeof(Views.TracePage));
                break;
            case "AoiUdt":
                ContentFrame.Navigate(typeof(Views.AoiUdtPage));
                break;
            case "Impact":
                ContentFrame.Navigate(typeof(Views.ImpactPage));
                break;
            case "Learning":
                ContentFrame.Navigate(typeof(Views.LearningPage));
                break;
        }
    }

    private async void OnStationRulesClick(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.FileTypeFilter.Add(".json");
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        var file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        AppState.StationRulesFileName = file.Name;

        if (ContentFrame.Content is Views.StationsPage page)
            page.UpdateFileName();

        var dialog = new ContentDialog
        {
            Title = "Station rules loaded",
            Content = $"Loaded: {file.Name}",
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    private async void OnImportClick(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.FileTypeFilter.Add(".L5X");
        picker.FileTypeFilter.Add(".l5x");
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        var file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        AppState.L5xFileName = file.Name;

        UpdateHeaderFileName();

        if (ContentFrame.Content is Views.NavigatorPage navigatorPage)
            navigatorPage.UpdateFileName();

        var dialog = new ContentDialog
        {
            Title = "L5X imported",
            Content = $"Loaded: {file.Name}",
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    private void UpdateHeaderFileName()
    {
        HeaderFileText.Text = string.IsNullOrWhiteSpace(AppState.L5xFileName)
            ? "Loaded: (none)"
            : $"Loaded: {AppState.L5xFileName}";
    }
}
