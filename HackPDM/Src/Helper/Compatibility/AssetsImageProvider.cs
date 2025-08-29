using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace HackPDM.Src.Helper.Compatibility
{
	public class AssetsImageProvider : IImageProvider
	{
		private readonly Dictionary<string, string> _assetMap;

		public AssetsImageProvider()
		{
			_assetMap = new Dictionary<string, string>
			{
				{ "wyrm", "ms-appx:///Assets/wyrm.png" },
				{ "phoenix", "ms-appx:///Assets/phoenix.png" },
				// Add more as needed
			};
		}

		public ImageSource? GetImage(string key)
		{
			return _assetMap.TryGetValue(key, out var uri) ? new BitmapImage(new Uri(uri)) : (ImageSource?)null;
		}

		public Bitmap? GetBitmap(string key) => throw new NotSupportedException("WinUI doesn't use System.Drawing");

		public IEnumerable<string> GetAvailableKeys() => _assetMap.Keys;
	}

}
