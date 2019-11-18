using System;
using Plugin.HybridWebView.Shared.Delegates;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RefreshSample : ContentPage
    {
        public RefreshSample()
        {
            InitializeComponent();
        }

        void OnRefreshClicked(object sender, EventArgs e)
        {
            WebContent.Refresh();
        }

        private void FormsWebView_OnNavigationStarted(object sender, DecisionHandlerDelegate e)
        {
            System.Diagnostics.Debug.WriteLine("Navigation has started");
        }

        private void FormsWebView_OnNavigationCompleted(object sender, System.EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Navigation has completed");
        }

        private void FormsWebView_OnContentLoaded(object sender, System.EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Content has loaded");
        }
    }
}