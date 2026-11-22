using System.Runtime.InteropServices;
using Avalonia.Input;

namespace Ssit.CrossX2.Editor.Helpers
{
    public static class Platform
    {
        public static bool ShowMenu => !RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>Modifier for adding objects to the selection: Cmd on macOS, Ctrl elsewhere.</summary>
        public static KeyModifiers MultiSelectModifier { get; } =
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? KeyModifiers.Meta : KeyModifiers.Control;

        public static bool IsMultiSelect(KeyModifiers modifiers) => modifiers.HasFlag(MultiSelectModifier);
    }
}
