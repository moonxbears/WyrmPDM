using System;
using System.Diagnostics;
using System.Threading.Tasks;

using HackPDM.Forms.Hack;

using Microsoft.UI.Xaml.Controls;

namespace HackPDM.Src.Helper.Xaml
{
	internal static class SafeHelper
	{
		internal static void SafeInvokeGen<T>(T data, Action<T> action)
		{
			HackFileManager.HackDispatcherQueue.TryEnqueue(() => action.Invoke(data));
		}
		
		internal static void SafeInvoker(Action action)
		{
			HackFileManager.HackDispatcherQueue.TryEnqueue(() =>
					{
						try
						{
							action.Invoke();
						}
						catch (Exception ex)
						{
							Debug.Fail(ex.Message, ex.StackTrace);
						}
					});
		}
		internal static Task SafeInvokerAsync(Action action)
		{
			var tcs = new TaskCompletionSource<bool>();
			HackFileManager.
					HackDispatcherQueue.TryEnqueue(() =>
					{
						try
						{
							action();
							tcs.SetResult(true);
						}
						catch (Exception ex)
						{
							tcs.SetException(ex);
						}
					});
			return tcs.Task;
		}
	}
}