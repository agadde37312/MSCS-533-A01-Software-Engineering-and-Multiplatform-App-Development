// App.xaml.cs
// Application entry point. Uses CreateWindow() (the modern MAUI 9 approach)
// instead of the deprecated MainPage property setter.

using LocationHeatMap.Views;

namespace LocationHeatMap
{
    /// <summary>
    /// Root application class. Bootstraps the navigation stack and
    /// injects the main page from the DI container.
    /// </summary>
    public partial class App : Application
    {
        private readonly MainPage _mainPage;

        /// <param name="mainPage">
        ///     Resolved by the MAUI DI container – avoids calling new MainPage()
        ///     directly which would bypass dependency injection.
        /// </param>
        public App(MainPage mainPage)
        {
            InitializeComponent();
            _mainPage = mainPage;
        }

        /// <summary>
        /// Creates the application window (replaces the obsolete MainPage setter).
        /// Wraps the injected page in a NavigationPage for a consistent title bar.
        /// </summary>
        protected override Window CreateWindow(IActivationState? activationState)
        {
            var navigationPage = new NavigationPage(_mainPage)
            {
                BarBackgroundColor = Color.FromArgb("#1A73E8"),
                BarTextColor       = Colors.White
            };

            return new Window(navigationPage);
        }
    }
}
