using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets;
public sealed partial class DialogCloseButton : UserControl
{
    public event EventHandler<RoutedEventArgs>? Click;

    public DialogCloseButton()
    {
        this.InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Click?.Invoke(sender, e);
    }
}
