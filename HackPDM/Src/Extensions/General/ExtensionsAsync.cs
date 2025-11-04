using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HackPDM.Src.Extensions.General
{
	public static class ExtensionsAsync
	{
		extension(MessageBox box)
		{
			public static Task<MessageBoxResult> ShowAsync(string message)
			{
				return Task.Run(() => MessageBox.Show(message));
			}
		}
	}
}
