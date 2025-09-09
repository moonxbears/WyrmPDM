using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

using Microsoft.Maui;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

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

		public Bitmap? GetBitmap(string key) => (Bitmap?)(_imageList.Images.ContainsKey(key) ? new Bitmap(_imageList.Images[key]) : null);

		public ImageSource? GetImage(string key) => throw new NotSupportedException("ImageSource Isn't supported");

		public IEnumerable<string> GetAvailableKeys() => _images.Keys;


		public async void SetImage(string key, byte[] imgBytes)
		{
			MemoryStream ms = new();
			await ms.WriteAsync(imgBytes);
			var Img = Image.FromStream(ms);
			_imageList.Images.Add(key, Img);
		}
	}

}
