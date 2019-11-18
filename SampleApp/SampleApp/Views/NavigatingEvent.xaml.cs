using SampleApp.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NavigatingEvent : ContentPage
    {

        public NavigatingEventViewModel ViewModel = new NavigatingEventViewModel();

        public NavigatingEvent()
        {
            InitializeComponent();
            BindingContext = ViewModel;
        }
    }
}