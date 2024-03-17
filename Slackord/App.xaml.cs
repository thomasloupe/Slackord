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
                if (appShell.Parent is Window window)
                {
                    window.Width = 1445;
                    window.Height = 750;
                }
            }
        }
    }
}
