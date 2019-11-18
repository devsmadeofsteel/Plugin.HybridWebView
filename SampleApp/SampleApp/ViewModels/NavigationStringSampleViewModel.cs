using Prism.Mvvm;
using System.Windows.Input;
using Prism.Commands;

namespace SampleApp.ViewModels
{
    public class NavigationStringSampleViewModel : BindableBase
    {
        ICommand _errorCommand;
        public ICommand ErrorCommand => (_errorCommand ?? (_errorCommand = new DelegateCommand(() => Uri = "<bd></asd>")));

        ICommand _reloadCommand;

        public ICommand ReloadCommand => (_reloadCommand ?? (_reloadCommand = new DelegateCommand(() =>
        {
            if (Uri.Equals(PageOne))
                Uri = PageTwo;
            else
                Uri = PageOne;
        })));

        const string PageOne = "<html><body><h1>Page One</h1></body></html>";
        const string PageTwo = "<html><body><h1>Page Two</h1></body></html>";

        string _uri = PageOne;

        public string Uri
        {
            get => _uri;
            set => SetProperty(ref _uri, value);
        }

        bool _isCancelled;

        public bool IsCancelled
        {
            get => _isCancelled;
            set => SetProperty(ref _isCancelled, value);
        }

    }
}
