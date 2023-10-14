using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace obs_net
{
    public partial class Obs
    {
        public enum LogErrorLevel { error = 100, warning = 200, info = 300, debug = 400 };

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        public delegate void log_handler_t(int lvl, string msg, IntPtr args, IntPtr p);

        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void base_set_log_handler(log_handler_t handler, IntPtr param);

        static public LogLevel LogErrorLvlToLogLvl(LogErrorLevel logError)
        {
            switch (logError)
            {
                case LogErrorLevel.error:
                    return LogLevel.Error;
                case LogErrorLevel.warning:
                    return LogLevel.Warning;
                case LogErrorLevel.info:
                    return LogLevel.Information;
                case LogErrorLevel.debug:
                    return LogLevel.Debug;
                default:
                    return LogLevel.None;
            }
        }
    }

    public static class va_list
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct VaListLinuxX64
        {
            uint gp_offset;
            uint fp_offset;
            IntPtr overflow_arg_area;
            IntPtr reg_save_area;
        }

        public static void UseStructurePointer<T>(T structure, Action<IntPtr> action)
        {
            var listPointer = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
            try
            {
                Marshal.StructureToPtr(structure, listPointer, false);
                action(listPointer);
            }
            finally
            {
                Marshal.FreeHGlobal(listPointer);
            }
        }

        public static void LinuxX64Callback(string format, IntPtr args, ILogger logger)
        {
            // The args pointer cannot be reused between two calls. We need to make a copy of the underlying structure.
            var listStructure = Marshal.PtrToStructure<VaListLinuxX64>(args);
            int byteLength = 0;
            UseStructurePointer(listStructure, listPointer =>
            {
                byteLength = vsnprintf(IntPtr.Zero, UIntPtr.Zero, format, listPointer) + 1;
            });
           var utf8Buffer = Marshal.AllocHGlobal(byteLength);
            try
            {
                UseStructurePointer(listStructure, listPointer =>
                {
                    vsprintf(utf8Buffer, format, listPointer);
                    logger.LogInformation(Utf8ToString(utf8Buffer));
                });
            }
            finally
            {
                Marshal.FreeHGlobal(utf8Buffer);
            }
       }

        public static string Utf8ToString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            var length = 0;

            while (Marshal.ReadByte(ptr, length) != 0)
            {
                length++;
            }

            byte[] buffer = new byte[length];
            Marshal.Copy(ptr, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        [DllImport("libc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int vsprintf(
            IntPtr buffer,
            [In][MarshalAs(UnmanagedType.LPStr)] string format,
            IntPtr args);

        [DllImport("libc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int vsnprintf(
            IntPtr buffer,
            UIntPtr size,
            [In][MarshalAs(UnmanagedType.LPStr)] string format,
            IntPtr args);

    }
}