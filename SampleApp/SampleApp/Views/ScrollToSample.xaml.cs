using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScrollToSample : ContentPage
    {
        public ScrollToSample()
        {
            InitializeComponent();
        }

        void ScrollUpButtonClicked(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () => await SampleWebview.InjectJavascriptAsync("window.scrollTo(0,document.body.scrollHeight);"));
        }

        void ScrollDownButtonClicked(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () => await SampleWebview.InjectJavascriptAsync("window.scrollTo(0,0);"));
        }
    }
}