// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Collections.Generic;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace DBus
{
	// Subclass obsolete BadAddressException to avoid ABI break
#pragma warning disable 0618
	//public class InvalidAddressException : Exception
	public class InvalidAddressException : BadAddressException
	{
		public InvalidAddressException (string reason) : base (reason) {}
	}
#pragma warning restore 0618

	[Obsolete ("Use InvalidAddressException")]
	public class BadAddressException : Exception
	{
		public BadAddressException (string reason) : base (reason) {}
	}

	static class Address
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

		enum TOKEN_INFORMATION_CLASS
		{
			TokenUser = 1,
			TokenGroups,
			TokenPrivileges,
			TokenOwner,
			TokenPrimaryGroup,
			TokenDefaultDacl,
			TokenSource,
			TokenType,
			TokenImpersonationLevel,
			TokenStatistics,
			TokenRestrictedSids,
			TokenSessionId,
			TokenGroupsAndPrivileges,
			TokenSessionReference,
			TokenSandBoxInert,
			TokenAuditPolicy,
			TokenOrigin
		}

		public struct TOKEN_USER
		{
			public SID_AND_ATTRIBUTES User ;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SID_AND_ATTRIBUTES
		{
			public IntPtr Sid ;
			public int Attributes ;
		}

		// Using IntPtr for pSID insted of Byte[]
		[DllImport("advapi32", CharSet=CharSet.Auto, SetLastError=true)]
		static extern bool ConvertSidToStringSid(IntPtr pSID, out IntPtr ptrSid);

		[DllImport("kernel32.dll")]
		static extern IntPtr LocalFree(IntPtr hMem);

		[DllImport("advapi32.dll", SetLastError=true)]
		static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, int TokenInformationLength, out int ReturnLength);

		// (unix:(path|abstract)=.*,guid=.*|tcp:host=.*(,port=.*)?);? ...
		// or
		// autolaunch:
		public static AddressEntry[] Parse (string addresses)
		{
			if (addresses == null)
				throw new ArgumentNullException (addresses);

			List<AddressEntry> entries = new List<AddressEntry> ();

			foreach (string entryStr in addresses.Split (';'))
				entries.Add (AddressEntry.Parse (entryStr));

			return entries.ToArray ();
		}

		public static string System
		{
			get {
				string addr = Environment.GetEnvironmentVariable ("DBUS_SYSTEM_BUS_ADDRESS");
				if (String.IsNullOrEmpty (addr) && OSHelpers.PlatformIsUnixoid)
					addr = "unix:path=/var/run/dbus/system_bus_socket";
				return addr;
			}
		}

		public static string GetSessionBusAddressFromSharedMemory ()
		{
			string result = OSHelpers.ReadSharedMemoryString ("DBusDaemonAddressInfo", 255);
			if (String.IsNullOrEmpty(result))
				result = OSHelpers.ReadSharedMemoryString ("DBusDaemonAddressInfoDebug", 255); // a DEBUG build of the daemon uses this different address...            
			return result;
		}

		public static string Session {
			get {
				// example: "tcp:host=localhost,port=21955,family=ipv4,guid=b2d47df3207abc3630ee6a71533effb6"
				// note that also "tcp:host=localhost,port=21955,family=ipv4" is sufficient

				// the predominant source for the address is the standard environment variable DBUS_SESSION_BUS_ADDRESS:
				string result = Environment.GetEnvironmentVariable ("DBUS_SESSION_BUS_ADDRESS");
				if (String.IsNullOrEmpty (result))
				{
					result = null;

				   	if (!OSHelpers.PlatformIsUnixoid) 
					{
						string prefix = "-";

						// Autolaunch
						Uri uri = new Uri(Assembly.GetExecutingAssembly().CodeBase);

        	                                int i = -1;
                	                        string installPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);

                        	                if ((i = installPath.ToLower().IndexOf ("lib")) != -1) {
                                	                installPath = installPath.Substring(0, i);
                                        	} else if ((i = installPath.ToLower().IndexOf ("bin")) != -1) {
	                                                installPath = installPath.Substring(0, i);
        	                                }

						string scope = "nonce";

						string dbusConfSessionPath = installPath + "etc" + Path.DirectorySeparatorChar + "dbus-1" + Path.DirectorySeparatorChar + "session.conf";
						if (File.Exists (dbusConfSessionPath))
						{
							XmlDocument doc = new XmlDocument();
							doc.Load(dbusConfSessionPath);


							XmlNodeList elemList = doc.GetElementsByTagName("listen");
							if (elemList.Count > 0)
							{
								XmlNode node = elemList[0];
								if (node != null)
								{
									string listenStr = node.InnerText;
									if (!String.IsNullOrEmpty (listenStr))
									{
										string[] listenArr = listenStr.Split ('=');
										if (listenArr.Length == 2)
										{
											scope = listenArr[1].Trim('*');
										}
									}
								}
							}
						}
						

						if (scope == "user")
						{
							int tokenInfLength = 0 ;
							bool res;

							// first call gets lenght of TokenInformation
							res = GetTokenInformation (WindowsIdentity.GetCurrent().Token, TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, tokenInfLength, out tokenInfLength);

							IntPtr tokenInformation = Marshal.AllocHGlobal (tokenInfLength);

							res = GetTokenInformation (WindowsIdentity.GetCurrent().Token, TOKEN_INFORMATION_CLASS.TokenUser, tokenInformation, tokenInfLength, out tokenInfLength);
							if (res)
							{
								TOKEN_USER tokenUser = (TOKEN_USER)Marshal.PtrToStructure (tokenInformation , typeof (TOKEN_USER)) ;

								IntPtr pstr = IntPtr.Zero;
								bool ok = ConvertSidToStringSid (tokenUser.User.Sid, out pstr);
								string sidstr = Marshal.PtrToStringAuto (pstr);
								LocalFree (pstr);

								prefix += sidstr;
							}

							Marshal.FreeHGlobal (tokenInformation);
						}
						else if (scope == "install-path")
						{
		        	                        using (SHA1 sha = new SHA1CryptoServiceProvider ())
		                	                {
		                        	                prefix += BitConverter.ToString (sha.ComputeHash(Encoding.ASCII.GetBytes (installPath.ToLower()))).Replace ("-", "").ToLower();
		                                	}
						}
						else
						{
							// On Windows systems, the dbus-daemon additionally uses shared memory to publish the daemon's address.
							// See function _dbus_daemon_publish_session_bus_address() inside the daemon.
							result = GetSessionBusAddressFromSharedMemory ();
							if (string.IsNullOrEmpty (result))
								prefix = String.Empty;
						}

						if (string.IsNullOrEmpty (result))
						{
							for (int j=0; j<2; j++)
							{
								IntPtr mapping = OpenFileMapping (FileRights.Read, false, "DBusDaemonAddressInfo"+prefix);
								if (mapping != IntPtr.Zero) {
									IntPtr p = MapViewOfFile (mapping, FileRights.Read, 0, 0, 0);
									if (p != IntPtr.Zero) {
										result = Marshal.PtrToStringAnsi (p);
										UnmapViewOfFile (p);
									}
									CloseHandle (mapping);
									if (result != null)
									{
										break;
									}
								}

								if (!File.Exists (installPath + "bin" + Path.DirectorySeparatorChar + "dbus-launch.exe"))
								{
									break;
								}

								ProcessStartInfo info = new ProcessStartInfo ();
								info.WorkingDirectory = installPath + "bin" + Path.DirectorySeparatorChar;
								info.FileName = installPath + "bin" + Path.DirectorySeparatorChar + "dbus-launch.exe";
								info.Arguments = "--session";
								info.LoadUserProfile = false;
								info.WindowStyle = ProcessWindowStyle.Hidden;
								info.UseShellExecute = false;
								info.CreateNoWindow = true;
								info.RedirectStandardError = true;
								info.RedirectStandardInput = true;
								info.RedirectStandardOutput = true;
								info.StandardErrorEncoding = Encoding.UTF8;
								info.StandardOutputEncoding = Encoding.UTF8;
				
								Process process = new Process ();
								process.StartInfo = info;
								process.Start ();
								process.WaitForExit (5000);
								Thread.Sleep(5000);
							}
						}
					}
				}

				return result;
			}
		}

		public static string Starter {
			get {
				return Environment.GetEnvironmentVariable ("DBUS_STARTER_ADDRESS");
			}
		}

		public static string StarterBusType {
			get {
				return Environment.GetEnvironmentVariable ("DBUS_STARTER_BUS_TYPE");
			}
		}
	}
}
