using System;
using Plugin.HybridWebView.Shared;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HeadersSample : ContentPage
    {
        public HeadersSample()
        {
            InitializeComponent();

            if (!HybridWebViewControl.GlobalRegisteredHeaders.ContainsKey("default-Global"))
                HybridWebViewControl.GlobalRegisteredHeaders.Add("default-Global", "default");

            if (!WebContent.LocalRegisteredHeaders.ContainsKey("default-Local"))
                WebContent.LocalRegisteredHeaders.Add("default-Local", "default");
        }

        void OnGlobalAdd(object sender, EventArgs e)
        {
            var globalKey = GlobalKey.Text;
            var globalValue = GlobalValue.Text;

            if (string.IsNullOrWhiteSpace(globalKey) || string.IsNullOrWhiteSpace(globalValue)) return;
            HybridWebViewControl.GlobalRegisteredHeaders.Add(globalKey, globalValue);

            GlobalKey.Text = "";
            GlobalValue.Text = "";
        }

        void OnLocalAdd(object sender, EventArgs e)
        {
            var localKey = LocalKey.Text;
            var localValue = LocalValue.Text;

            if (string.IsNullOrWhiteSpace(localKey) || string.IsNullOrWhiteSpace(localValue)) return;
            WebContent.LocalRegisteredHeaders.Add(localKey, localValue);

            LocalKey.Text = "";
            LocalValue.Text = "";
        }

        void OnReloadClicked(object sender, EventArgs e)
        {
            WebContent.Refresh();
        }
    }
}