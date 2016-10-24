using System;
using System.Runtime.InteropServices;
using System.Reflection;

namespace DBus
{
	internal class OSHelpers
	{
		enum FileRights : uint          // constants from winbase.h
		{
			Read = 4,
			Write = 2,
			ReadWrite = Read + Write
		}

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern IntPtr OpenFileMapping (FileRights dwDesiredAccess,
						      bool bInheritHandle,
						      string lpName);
		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern IntPtr MapViewOfFile (IntPtr hFileMappingObject,
						    FileRights dwDesiredAccess,
						    uint dwFileOffsetHigh,
						    uint dwFileOffsetLow,
						    uint dwNumberOfBytesToMap);
		[DllImport ("Kernel32.dll")]
		static extern bool UnmapViewOfFile (IntPtr map);

		[DllImport ("kernel32.dll")]
        	static extern int CloseHandle (IntPtr hObject);

		static PlatformID platformid = Environment.OSVersion.Platform;

		public static bool PlatformIsUnixoid
		{
			get {
				switch (platformid) {
					case PlatformID.Win32S:       return false;
					case PlatformID.Win32Windows: return false;
					case PlatformID.Win32NT:      return false;
					case PlatformID.WinCE:        return false;
					case PlatformID.Unix:         return true;
					case PlatformID.Xbox:         return false;
					case PlatformID.MacOSX:       return true;
					default:                      return false;
				}
			}
		}

		// Reads a string from shared memory with the ID "id".
		public static string ReadSharedMemoryString (string id)
		{
			string result = null;

			IntPtr mapping = OpenFileMapping (FileRights.Read, false, id);
			if (mapping != IntPtr.Zero) 
			{
				IntPtr p = MapViewOfFile (mapping, FileRights.Read, 0, 0, 0);
				if (p != IntPtr.Zero) 
				{
					result = Marshal.PtrToStringAnsi (p);
					UnmapViewOfFile (p);
				}
			}
			CloseHandle (mapping);

			return result;
		}
	}
}
