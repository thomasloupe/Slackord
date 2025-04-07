namespace Slackord
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            // Use CreateWindow instead of setting MainPage directly
            Window window = new(new NavigationPage(new MenuApp.MainPage()));
            return window;
        }
    }
}