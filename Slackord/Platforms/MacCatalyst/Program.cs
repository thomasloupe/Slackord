using ObjCRuntime;
using UIKit;

namespace Slackord
{
    /// <summary>
    /// Mac Catalyst program entry point that initializes the UIApplication with the AppDelegate.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point for the Mac Catalyst application.
        /// </summary>
        /// <param name="args">Command line arguments passed to the application.</param>
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
