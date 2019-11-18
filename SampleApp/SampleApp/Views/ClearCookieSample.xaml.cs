using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ClearCookieSample : ContentPage
    {
        public ClearCookieSample()
        {
            InitializeComponent();
        }
        private async void ClearCookiesClicked(object sender, EventArgs e)
        {
            await localContent.ClearCookiesAsync();
        }

        void OnRefreshPageClicked(object sender, EventArgs e)
        {
            localContent.Refresh();
        }
    }
}