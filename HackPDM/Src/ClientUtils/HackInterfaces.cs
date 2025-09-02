using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace HackPDM
{
    public interface IConvert<T>
    {
        T ConvertFromHT(Hashtable ht);
    }
    public interface IRowData { }
	public interface ITreeItem
	{
		public object? Tag { get; set; }
		public List<ITreeItem> Children { get; set; }
	}
	public interface IListItem<T>
	{
		public T? Value { get; }
		public bool IsSelected { get; set; }
	}
	public interface ISettingsProvider
	{
		T? Get<T>(string key, T? defaultValue = default);
		void Set<T>(string key, T value);
	}
	public interface IImageProvider
	{
		ImageSource? GetImage(string key);
		Bitmap? GetBitmap(string key);
		void SetImage(string key, byte[] imgBytes);
		IEnumerable<string> GetAvailableKeys();
	}
}
