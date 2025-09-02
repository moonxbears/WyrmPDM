using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Storage;
using Windows.Storage.Streams;

namespace HackPDM.Src.Helper.Compatibility
{
	public class AssetsImageProvider : IImageProvider
	{
		

		internal readonly Dictionary<string, string>? AssetMap;
		private static StorageFolder Storage => ApplicationData.Current.LocalFolder;
		public AssetsImageProvider() : this(new Dictionary<string, string>()) {}
		public AssetsImageProvider(Dictionary<string, string>? assetMap)
		{
			this.AssetMap = assetMap;
		}
		public ImageSource? GetImage(string key)
		{
			return AssetMap.TryGetValue(key, out var uri) ? new BitmapImage(new Uri(uri)) : (ImageSource?)null;
		}
		public async void SetImage(string key, byte[] imageBytes)
		{
			StorageFolder localfolder = await Storage.CreateFolderAsync("Assets/Images", CreationCollisionOption.OpenIfExists);
			StorageFile imgFile = await localfolder.CreateFileAsync(key, CreationCollisionOption.ReplaceExisting);
			using var fileStream = await imgFile.OpenAsync(FileAccessMode.ReadWrite);
			await fileStream.WriteAsync(imageBytes.AsBuffer());
			AssetMap.Add(key, Path.Combine(StorageBox.LOCALPREFIX, StorageBox.ASSETSFOLDER, StorageBox.IMAGEFOLDER, key));
		}

		public IEnumerable<string> GetAvailableKeys() => AssetMap.Keys;
		public Bitmap? GetBitmap(string key) => throw new NotSupportedException("WinUI doesn't use System.Drawing");
		public void SetImage(string key, Bitmap bitmap) => throw new NotSupportedException("WinUI doesn't use System.Drawing");

	}

}
