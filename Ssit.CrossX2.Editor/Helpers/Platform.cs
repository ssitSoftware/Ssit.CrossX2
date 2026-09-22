using System.Runtime.InteropServices;

namespace Ssit.CrossX2.Editor.Helpers
{
    public static class Platform
    {
        public static bool ShowMenu => !RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}