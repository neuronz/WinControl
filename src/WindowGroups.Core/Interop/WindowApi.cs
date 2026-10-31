using System.Text;

namespace WindowGroups.Core.Interop;

/// <summary>Production <see cref="IWindowApi"/> backed by Win32.</summary>
public sealed class WindowApi : IWindowApi
{
    public IReadOnlyList<WindowHandleInfo> EnumerateTopLevelWindows()
    {
        var result = new List<WindowHandleInfo>();
        _ = NativeMethods.EnumWindows((hWnd, lParam) =>
        {
            _ = NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
            result.Add(new WindowHandleInfo(hWnd, unchecked((int)processId)));
            return true;
        }, IntPtr.Zero);

        return result;
    }

    public string? GetWindowTitle(nint handle)
    {
        var builder = new StringBuilder(512);
        var length = NativeMethods.GetWindowText(handle, builder, builder.Capacity);
        return length > 0 ? builder.ToString() : null;
    }

    public bool IsWindow(nint handle) => NativeMethods.IsWindow(handle);

    public bool IsWindowVisible(nint handle) => NativeMethods.IsWindowVisible(handle);

    public bool IsIconic(nint handle) => NativeMethods.IsIconic(handle);

    public bool IsCloaked(nint handle)
    {
        var hr = NativeMethods.DwmGetWindowAttribute(handle, NativeMethods.DWM_WA_CLOAKED, out var cloaked, sizeof(int));
        return hr == 0 && cloaked != 0;
    }

    public bool HasToolWindowStyle(nint handle)
    {
        var extendedStyle = NativeMethods.GetWindowLong(handle, NativeMethods.GWL_EXSTYLE);
        return (extendedStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0;
    }

    public void Minimize(nint handle) => _ = NativeMethods.ShowWindow(handle, NativeMethods.SW_MINIMIZE);

    public void Restore(nint handle) => _ = NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);
}