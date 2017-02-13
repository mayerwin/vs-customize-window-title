using System.Security.Principal;

namespace ErwinMayerLabs.Lib
{
    public static class Elevated
    {
        public static bool IsElevated()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
