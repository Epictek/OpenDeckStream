using System;
using System.Runtime.InteropServices;

public static class UnixSysCalls {

[DllImport("libwayland-client.so")]
public static extern IntPtr wl_display_connect(IntPtr display);

[DllImport("libX11.so.6")]
public static extern IntPtr XOpenDisplay(IntPtr display);
}