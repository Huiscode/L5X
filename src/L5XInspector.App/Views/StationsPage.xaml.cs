using Microsoft.UI.Xaml.Controls;

namespace L5XInspector.App.Views;

public sealed partial class StationsPage : Page
{
    public StationsPage()
    {
        InitializeComponent();
        UpdateFileName();
    }

    public void UpdateFileName()
    {
        var file = AppState.StationRulesFileName;
        if (string.IsNullOrWhiteSpace(file))
            file = "Loaded file: (none)";
        else
            file = $"Loaded file: {file}";

        var textBlock = (TextBlock)FindName("StationRulesFileText");
        if (textBlock is not null)
            textBlock.Text = file;
    }
}
