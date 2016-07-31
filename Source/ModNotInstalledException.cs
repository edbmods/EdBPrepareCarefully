using System;

namespace EdB.PrepareCarefully
{
	public class ModNotInstalledException : Exception
	{
		public ModNotInstalledException() { }
		public ModNotInstalledException(String message) : base(message) { }	
		public ModNotInstalledException(String message, Exception cause) : base(message, cause) { }	
	}
}

