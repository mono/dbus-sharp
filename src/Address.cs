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

		public static string GetSessionBusAddressFromSharedMemory (string suffix = "")
		{
			string result = OSHelpers.ReadSharedMemoryString (string.Format("DBusDaemonAddressInfo{0}", suffix));
			if (String.IsNullOrEmpty(result))
				result = OSHelpers.ReadSharedMemoryString (string.Format("DBusDaemonAddressInfoDebug{0}", suffix)); // a DEBUG build of the daemon uses this different address...            
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
						// On Windows systems, the dbus-daemon additionally uses shared memory to publish the daemon's address.
						// See function _dbus_daemon_publish_session_bus_address() inside the daemon.
						result = GetSessionBusAddressFromSharedMemory ();

						if (string.IsNullOrEmpty (result))
						{
							string suffix = "-";
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
								ConvertSidToStringSid (tokenUser.User.Sid, out pstr);
								string sidstr = Marshal.PtrToStringAuto (pstr);
								LocalFree (pstr);

								suffix += sidstr;
							}

							Marshal.FreeHGlobal (tokenInformation);

							result = GetSessionBusAddressFromSharedMemory (suffix);
						}

						if (string.IsNullOrEmpty (result))
						{
							string suffix = "-";
							string installPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);

		        	                        using (SHA1 sha = new SHA1CryptoServiceProvider ())
		                	                {
		                        	                suffix += BitConverter.ToString (sha.ComputeHash(Encoding.ASCII.GetBytes (installPath.ToLower()))).Replace ("-", "").ToLower();
		                                	}

							result = GetSessionBusAddressFromSharedMemory (suffix);
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
