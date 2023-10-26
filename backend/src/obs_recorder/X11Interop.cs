using System;
using System.Runtime.InteropServices;

public static class X11Interop
{
    [DllImport("libX11", EntryPoint = "XOpenDisplay")]
    public static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11", EntryPoint = "XDefaultScreen")]
    public static extern int XDefaultScreen(IntPtr display);

    [DllImport("libX11", EntryPoint = "XDisplayHeight")]
    public static extern int XDisplayHeight(IntPtr display, int screen_number);

    [DllImport("libX11", EntryPoint = "XDisplayWidth")]
    public static extern int XDisplayWidth(IntPtr display, int screen_number);

    [DllImport("libX11", EntryPoint = "XCloseDisplay")]
    public static extern int XCloseDisplay(IntPtr display);

    public static (int width, int height) GetSize(string server = ":0")
    {
        try
        {
            IntPtr display = XOpenDisplay(IntPtr.Zero);

            if (display == IntPtr.Zero)
            {
                Console.WriteLine("Unable to open display.");
                return (0, 0);
            }

            int screen_number = XDefaultScreen(display);
            int width = XDisplayWidth(display, screen_number);
            int height = XDisplayHeight(display, screen_number);

            XCloseDisplay(display);

            return (width, height);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return (0, 0);
        }
    }

    [DllImport("libX11", EntryPoint = "XGetAtomName")]
    public static extern IntPtr XGetAtomName(IntPtr display, IntPtr atom);

    [DllImport("libX11", EntryPoint = "XInternAtom")]
    public static extern IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);

    [DllImport("libX11", EntryPoint = "XGetWindowProperty")]
    public static extern int XGetWindowProperty(IntPtr display, IntPtr w, IntPtr property, IntPtr long_offset, IntPtr long_length, bool delete, IntPtr req_type, out IntPtr actual_type_return, out int actual_format_return, out IntPtr nitems_return, out IntPtr bytes_after_return, out IntPtr prop_return);

    [DllImport("libX11", EntryPoint = "XFree")]
    public static extern void XFree(IntPtr data);

    public static void PrintAtomNames(IntPtr display, IntPtr window)
    {
        IntPtr atom = XInternAtom(display, "_NET_SUPPORTED", true);
        XGetWindowProperty(display, window, atom, IntPtr.Zero, new IntPtr(1024), false, new IntPtr(4) /* AnyPropertyType */, out IntPtr actual_type, out int actual_format, out IntPtr nitems, out IntPtr bytes_after, out IntPtr prop);

        for (var i = 0; i < nitems.ToInt64(); i++)
        {
            IntPtr atom_ptr = Marshal.ReadIntPtr(prop, i * IntPtr.Size);
            Console.WriteLine(Marshal.PtrToStringAnsi(XGetAtomName(display, atom_ptr)));
        }

        XFree(prop);
    }

}