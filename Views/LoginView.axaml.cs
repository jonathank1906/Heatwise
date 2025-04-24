using Avalonia.Controls;
using Sem2Proj.ViewModels;

namespace Sem2Proj.Views;

public partial class LoginView : Window
{
    public LoginView()
    {
        InitializeComponent();
        DataContext = new LoginViewModel(this);
    }
}
