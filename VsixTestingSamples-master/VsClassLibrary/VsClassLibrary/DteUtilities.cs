using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio.Shell;

namespace VsClassLibrary
{
    public class DteUtilities
    {
        public static string GetVsVersion()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE)ServiceProvider.GlobalProvider.GetService(typeof(DTE));
            Assumes.Present(dte);
            return dte.Version;
        }
    }
}
