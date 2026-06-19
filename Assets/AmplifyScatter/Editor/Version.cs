// Amplify Scatter FREE
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;

namespace AmplifyScatter
{
	[Serializable]
	public class Version
	{
		public const byte Major = 1;
		public const byte Minor = 0;
		public const byte Release = 0;

		public static string StaticToString()
		{
			return string.Format( "{0}.{1}.{2}", Major, Minor, Release );
		}

		public static int FullNumber { get { return Major * 1000000 + Minor * 10000 + Release; } }
		public static string FullLabel { get { return "Version=" + FullNumber; } }
	}
}