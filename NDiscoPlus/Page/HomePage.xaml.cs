using NDiscoPlus.ViewModel;

namespace NDiscoPlus.Page;

public partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}