using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NavigationPageStart : ContentPage
    {
        public NavigationPageStart()
        {
            InitializeComponent();
        }

        void Button_Clicked(object sender, EventArgs e)
        {
            ((NavigationPage)Application.Current.MainPage).PushAsync(new InternetSample());
        }
    }
}