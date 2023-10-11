using MenuApp;

namespace Slackord
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
        protected override void OnStart()
        {
            // Always call the base method when overriding
            base.OnStart();

            // Set window size
            if (MainPage is AppShell appShell)
            {
                Window window = appShell.Parent as Window;
                if (window != null)
                {
                    window.Width = 1175;
                    window.Height = 750;
                }
            }
        }
    }
}
