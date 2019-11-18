using System;
using Plugin.HybridWebView.Shared;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class JavascriptInternet : ContentPage
    {
        public JavascriptInternet()
        {
            InitializeComponent();

            InjectionText.Text = "document.body.style.backgroundColor = \"red\";";
            GlobalText.Text = "globalCallback('Hello from Javascript!');";
            LocalText.Text = "localCallback('Hello from Javascript!');";

            HybridWebViewControl.AddGlobalCallback("globalCallback", GlobalCallback);
            WebContent.AddLocalCallback("localCallback", LocalCallback);
        }

        void GlobalCallback(string obj)
        {
            System.Diagnostics.Debug.WriteLine($"Got global callback: {obj}");
        }

        void LocalCallback(string obj)
        {
            System.Diagnostics.Debug.WriteLine($"Got local callback: {obj}");
        }

        async void OnInjectionClicked(object sender, EventArgs e)
        {
            var text = InjectionText.Text;
            var response = await WebContent.InjectJavascriptAsync(text);

            System.Diagnostics.Debug.WriteLine($"Got javascript response: {response}");
        }

        void GlobalCallbackClicked(object sender, EventArgs e)
        {
            var text = GlobalText.Text;
            WebContent.InjectJavascriptAsync(text).ConfigureAwait(false);
        }

        void LocalCallbackClicked(object sender, EventArgs e)
        {
            var text = LocalText.Text;
            WebContent.InjectJavascriptAsync(text).ConfigureAwait(false);
        }
    }
}