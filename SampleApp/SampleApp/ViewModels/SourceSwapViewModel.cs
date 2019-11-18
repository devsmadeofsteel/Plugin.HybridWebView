using Prism.Mvvm;

namespace SampleApp.ViewModels
{
    public class SourceSwapViewModel : BindableBase
    {
        string _uri = "https://www.google.co.uk";

        public string Uri
        {
            get => _uri;
            set => SetProperty(ref _uri, value);
        }
    }
}
