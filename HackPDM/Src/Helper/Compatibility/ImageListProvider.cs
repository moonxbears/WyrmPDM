using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

using Microsoft.UI.Xaml.Media;

namespace HackPDM.Src.Helper.Compatibility
{
	public class ImageListProvider : IImageProvider
	{
		private readonly Dictionary<string, Bitmap?> _images;
		private readonly ImageList _imageList;
		public ImageListProvider()
		{
			_images = [];
			_imageList = new();
		}

		public Bitmap? GetBitmap(string key) => _images.TryGetValue(key, out var bmp) ? bmp : null;

		public ImageSource? GetImage(string key) => throw new ArgumentOutOfRangeException();

		public IEnumerable<string> GetAvailableKeys() => _images.Keys;
	}

}
