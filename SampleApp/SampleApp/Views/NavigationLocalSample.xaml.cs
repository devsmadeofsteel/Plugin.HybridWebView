using Plugin.HybridWebView.Shared.Delegates;
using SampleApp.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NavigationLocalSample : ContentPage
    {
        NavigationEventsLocalViewModel ViewModel = new NavigationEventsLocalViewModel();

        public NavigationLocalSample()
        {
            InitializeComponent();
            BindingContext = ViewModel;
        }

        private void FormsWebView_OnNavigationStarted(object sender, DecisionHandlerDelegate e)
        {
            System.Diagnostics.Debug.WriteLine("Navigation has started");
            System.Diagnostics.Debug.WriteLine($"Will cancel: {ViewModel.IsCancelled}");

            e.Cancel = ViewModel.IsCancelled;
        }

        private void FormsWebView_OnNavigationCompleted(object sender, System.EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Navigation has completed");
        }

        private void FormsWebView_OnContentLoaded(object sender, System.EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Content has loaded");
        }

        private void FormsWebView_OnNavigationError(object sender, int e)
        {
            System.Diagnostics.Debug.WriteLine($"An error was thrown with code: {e}");
        }
    }
}