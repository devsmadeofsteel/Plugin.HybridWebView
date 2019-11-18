using System;
using SampleApp.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SourceSwapSample : ContentPage
    {
        SourceSwapViewModel ViewModel = new SourceSwapViewModel();

        public SourceSwapSample()
        {
            InitializeComponent();
            BindingContext = ViewModel;
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            ViewModel.Uri = EntryField.Text;
        }
    }
}