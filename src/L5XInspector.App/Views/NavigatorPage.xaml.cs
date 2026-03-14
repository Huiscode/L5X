using Microsoft.UI.Xaml.Controls;

namespace L5XInspector.App.Views;

public sealed partial class NavigatorPage : Page
{
    public NavigatorPage()
    {
        InitializeComponent();
        UpdateFileName();
    }

    public void UpdateFileName()
    {
        L5xFileText.Text = AppState.L5xFileName;
    }
}
