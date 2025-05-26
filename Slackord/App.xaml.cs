namespace Slackord
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates the main application window with a navigation page
        /// </summary>
        /// <param name="activationState">The activation state of the app</param>
        /// <returns>The main application window</returns>
        protected override Window CreateWindow(IActivationState activationState)
        {
            const int windowWidth = 1280;
            const int windowHeight = 800;

            var window = new Window(new NavigationPage(new MenuApp.MainPage()))
            {
                Width = windowWidth,
                Height = windowHeight,
                MinimumWidth = windowWidth,
                MinimumHeight = windowHeight,
                MaximumWidth = windowWidth,
                MaximumHeight = windowHeight
            };

#if WINDOWS
            window.HandlerChanged += (s, e) =>
            {
                if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
                    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                    appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);
                    appWindow.Resize(new Windows.Graphics.SizeInt32
                    {
                        Width = windowWidth,
                        Height = windowHeight
                    });

                    if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                    {
                        presenter.IsResizable = false;
                        presenter.IsMaximizable = false;
                    }
                }
            };
#endif

            return window;
        }
    }
}
