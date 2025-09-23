using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.UI.Xaml.Controls;

namespace HackPDM.Src.ClientUtils.Types
{
    public static class InstanceManager
    {
        private static readonly Dictionary<Type, List<Page>> _pages = [];

        public static T GetAPage<T>() where T : Page, new()
        {
            if (_pages.TryGetValue(typeof(T), out var page))
                return (T)page.First();

            T newPage = new T();
            List<Page> newPages = [newPage];
            _pages[typeof(T)] = newPages;
            return newPage;
        }
		public static List<T> GetPages<T>() where T : Page, new()
		{
			if (_pages.TryGetValue(typeof(T), out var page))
				return [.. page.Cast<T>()];

			T newPage = new();
			List<T> newPages = [newPage];
			_pages[typeof(T)] = [.. newPages.OfType<Page>()];
			return newPages;
		}

        public static void Register<T>(T page) where T : Page
        {
            if (_pages.TryGetValue(typeof(T), out var val))
            {
                if (!val?.Contains(page) == false) val!.Add(page);
            }
            else
            {
                _pages[typeof(T)] = [page];
            }
            //_pages[typeof(T)] = page;
        }

        public static bool TryGet<T>(out T? page) where T : Page
        {
            if (_pages.TryGetValue(typeof(T), out var result))
            {
                if (result.First() is not null)
                {
                    page = (T)result.First();
                    return true;
                }
            }
            page = null;
            return false;
        }

		public static bool TryGetPages<T>(out List<T>? pages) where T : Page
		{
			if (_pages.TryGetValue(typeof(T), out var result))
			{
				if (result.Count != 0)
				{
					pages = result.Cast<T>().ToList();
					return true;
				}
			}
			pages = null;
			return false;
		}
    }

}
