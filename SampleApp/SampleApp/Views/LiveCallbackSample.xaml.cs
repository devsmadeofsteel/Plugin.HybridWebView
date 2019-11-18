using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LiveCallbackSample : ContentPage
    {
        public LiveCallbackSample()
        {
            InitializeComponent();
        }

        void AddCallback(object sender, EventArgs e)
        {
            WebContent.AddLocalCallback("localCallback", HandleCallback);
        }

        void CallCallback(object sender, EventArgs e)
        {
            WebContent.InjectJavascriptAsync("localCallback('Hello World');").ConfigureAwait(false);
        }

        void HandleCallback(string obj)
        {
            System.Diagnostics.Debug.WriteLine($"Got callback: {obj}");
        }
    }
}