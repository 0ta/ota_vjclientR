using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace ota.ndi
{
	public static partial class NativeDirPickerlib
	{
	    [DllImport("__Internal")]
        internal static extern void pickDirWithSecurityScope();

        [DllImport("__Internal")]
        internal static extern string getSecurityScopeURL();

        [DllImport("__Internal")]
        internal static extern int getSecurityScopeBookmark(out IntPtr p);        

	}

}

