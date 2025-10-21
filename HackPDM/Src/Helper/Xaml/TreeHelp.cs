using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.WinUI.UI.Controls;

using HackPDM.ClientUtils;
using HackPDM.Data;
using HackPDM.Extensions.Controls;
using HackPDM.Extensions.General;
using HackPDM.Forms.Hack;
using HackPDM.Hack;
using HackPDM.Odoo;
using HackPDM.Odoo.OdooModels.Models;
using HackPDM.Properties;
using HackPDM.Src.ClientUtils.Types;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Storage.Streams;

namespace HackPDM.Src.Helper.Xaml
{
	public class TreeHelp
	{
		private HackFileManager _HFM { get; init; }
		private TreeView _tree { get; init; }
		internal TreeHelp(HackFileManager HackFM)
		{
			_HFM = HackFM;
			_tree = HackFM.GetOdooDirectoryTree();
		}

		#region TreeView functions
		// tree view directories
		internal async Task CreateTreeViewBackground(TreeView tree)
		{
			await LoadOdooDirectoryTree(tree);
		}
		internal async Task LoadOdooDirectoryTree(TreeView tree)
		{
			_HFM.IsTreeLoaded = false;

			try
			{
				await SafeHelper.SafeInvokerAsync(tree, async void () =>
				{
					try
					{
						await CreateTreeHash(tree, _HFM._root);
						// Debug.WriteLine("");
						// foreach (EntryRow row in OdooDirectoryTree.ItemsSource as ObservableCollection<EntryRow>)
						// {
						// 	Debug.WriteLine(row.Name);
						// }
						// Debug.WriteLine("");
						CreateLocalTree(tree);

						if (_HFM.LastSelectedNode != null)
						{
							_HFM.LastSelectedNode = tree.FindTreeNode(_HFM.LastSelectedNodePath)?.Node;
						}

						var tData = tree.RootNodes;
						foreach (var n in tData)
						{
							n.LinkedData.SortTree();
						}

						_HFM.LastSelectedNode?.LinkedData.EnsureVisible(tree);
					}
					catch (Exception e)
					{
						Debug.Fail(e.Message);
					}
				});
				_HFM.IsTreeLoaded = true;
			}
			catch (Exception exception)
			{
				Debug.WriteLine(exception);
			}
			//SafeInvoke(OdooEntryImage, () =>
			//{
			//	//OdooEntryImage.Image = null;
			//});
		}
		internal async Task<(Hashtable entries, Dictionary<string, Task<HackFile>> hackmap)> GetHackAndEntry(int? directoryId)
		{
			if (directoryId is null) return (null, null);
			Hashtable entries = await Task.Run(() => HpDirectory.GetEntries(directoryId, _HFM.IsActive));
			Dictionary<string, Task<HackFile>> hackFileMap = await GridHelp.GetFileMap(entries);
			return (entries, hackFileMap);
		}
		internal async Task TreeSelectItem(TreeView tree, TreeViewNode node, DataGrid grid, CancellationToken token = default)
		{
			_HFM.IsListLoaded = false;
			await AsyncHelper.WaitUntil(() => _HFM.IsTreeLoaded, 100, -1, token);
			node.LinkedData.EnsureVisible(tree);
			GridHelp.InitGridView(grid);

			try
			{
				TreeData? tData = node?.Content as TreeData;
				if (tData is not null)
				{
					if (tData?.DirectoryId is null or 0)
					{
						// add file entries to folder
						AddLocalEntries(grid, node);
						return;
					}

					token.ThrowIfCancellationRequested();
					(Hashtable entries, Dictionary<string, Task<HackFile>> hackmap) = await GetHackAndEntry(tData.DirectoryId);
					token.ThrowIfCancellationRequested();
					AddRemoteEntries(grid, entries, hackmap);
					ListView items = new();

					AddLocalEntries(grid, _HFM.LastSelectedNode, hackmap);

					_HFM.OEntries.Sort((EntryRow x, EntryRow y) => string.Compare(x.Name, y.Name));
				}
				_HFM.IsListLoaded = true;
			}
			catch { }
		}
		internal async Task<bool> TreeItemsChangedPolling(int timeout = -1, CancellationToken token = default)
		{
			while (!token.IsCancellationRequested && _HFM.EntryPollingMs > 0)
			{
				bool isLoaded = await AsyncHelper.WaitUntil(() => _HFM.IsTreeLoaded && _HFM.IsListLoaded, 1000, -1, token);

				if (!isLoaded || _HFM.LastSelectedNode is null) continue;

				await Task.Delay(_HFM.EntryPollingMs, token);
			}
			return false;
		}
		internal static async Task CreateTreeHash(TreeView tree, HpDirectory directory)
		{
			await AddDirectoriesToTree(tree, directory.GetSubdirectories(false));
		}
		internal static void CreateLocalTree(in TreeView treeView)
		{
			Dictionary<string, TreeViewNode>? treeDict = Help.ConvertTreeToDictionary(treeView);
			IEnumerable<string> pathways = Directory.EnumerateDirectories(HackDefaults.PwaPathAbsolute, "*", SearchOption.AllDirectories);
			pathways = Help.FastSlice(pathways, HackDefaults.PwaPathAbsolute.Length, prependText: "root");

			foreach (string pathway in pathways)
			{
				string[] paths = pathway.Split('\\');
				(int, TreeViewNode?) validIndexNode = Help.LastValidTreeIndex(in pathway, in paths, treeDict);
				// the last valid index does not go to the end meaning it didn't find the
				// remaining paths
				if (validIndexNode.Item1 != paths.Length - 1)
				{
					AddLocalDirectories(treeView, validIndexNode.Item2, paths.AsSpan(validIndexNode.Item1 + 1), treeDict);
				}
			}
		}
		internal static void AddLocalDirectories(TreeView tree, TreeViewNode node, Span<string> pathway, Dictionary<string, TreeViewNode> treeDict)
		{
			string[] paths = [.. pathway];
			SafeHelper.SafeInvoker(tree, () =>
			{
				for (int i = 0; i < paths.Length; i++)
				{
					var parentData = node?.Content as TreeData;
					TreeViewNode tNode = new();
					var newNode = tNode.LinkedData;
					newNode.Name = paths[i];
					newNode.Icon = Assets.GetImage("simple-folder-icon_32.gif") as BitmapImage;
					newNode.DirectoryId = 0;

					node?.Children.Add(tNode);
					treeDict.TryAdd(newNode?.FullPath ?? "", tNode);
					//treeDict.Add(parentData?.FullPath ?? "", node);
				}
			});

		}
		internal static async Task AddDirectoriesToTree(TreeView tree, Hashtable entries)
		{
			await SafeHelper.SafeInvokerAsync(tree, () =>
			{
				tree.RootNodes.Clear();
				var child = RecurseAddNodesAsync(entries);
				child.Wait();
				if (child.Result.Item1 is null) return;
				tree.RootNodes.Add(child.Result.Item1);
			});
		}
		internal static async Task<(TreeViewNode?, TreeData)> RecurseAddNodesAsync(Hashtable node, int depth = 0)
		{
			// add container node (directory name)

			// refresh to show active changes
			// add children
			TreeViewNode treeNodeName = new();
			if (node["directories"] is Hashtable { Count: > 0 } directory)
			{
				foreach (DictionaryEntry pair in directory)
				{
					if (pair.Value is not Hashtable childDirectory) continue;
					var child = await RecurseAddNodesAsync(childDirectory, depth + 1);
					if (child.Item1 is not null) treeNodeName.Children.Add(child.Item1);
				}
			}
			var dat = treeNodeName.LinkedData;
			dat.Name = node["name"] as string;
			dat.DirectoryId = node["id"] as int?;
			// if treeNode == null then it will be the root node
			string? fullPath;
			if (dat.FullPath is null)
				fullPath = dat?.Name;
			else
				fullPath = dat?.FullPath;
			string path = HackDefaults.DefaultPath(fullPath ?? "root", true);

			if (!Directory.Exists(path))
			{
				dat.Icon = Assets.GetImage("folder-icon_remoteonly_32") as BitmapImage;
			}

			return (treeNodeName, dat);
		}
		public static void RefreshTree(TreeView tree)
			=> SafeHelper.SafeInvoker(tree, tree.UpdateLayout);
		public async Task RestartTree(TreeView tree)
			=> await CreateTreeViewBackground(tree);
		public void RestartEntries(TreeView tree, DataGrid grid)
		{
			if (_HFM.LastSelectedNode is null) return;
			SafeHelper.SafeInvoker(tree, async () => await TreeSelectItem(tree, _HFM.LastSelectedNode!, grid));
			SafeHelper.SafeInvoker(grid, async () =>
			{
				await AsyncHelper.WaitUntil(() => _HFM.IsListLoaded);
				if (grid.SelectedItems is not null and { Count: > 0 } items)
				{
					var entry = items[0] as EntryRow;
					if (entry is not null) grid.ScrollIntoView(entry, grid.Columns.First());
				}
			});
		}
		#endregion
		#region Tree Item Selection
		private static void FindUpdatedEntries(DataGrid grid, TreeViewNode node, Hashtable entries, Dictionary<string, Task<HackFile>> hackFileMap)
		{
			// things to check for:
			// 1. if the entry is in the entries hashtable
			// 2. if the entry is in the hackFileMap
			// 3. if the entry is not in the entries hashtable but is in the hackFileMap
			// 4. if the entry is not in the hackFileMap but is in the entries hashtable
			// 5. if the entry is not in either the entries hashtable or the hackFileMap
			// 6. if the entry is in both the entries hashtable and the hackFileMap but has been modified locally
			// 7. if the entry is in both the entries hashtable and the hackFileMap but has been modified remotely

			HackFile[]? files = GetHackNonEntries(node, hackFileMap);
			ObservableCollection<EntryRow>? items = grid.ItemsSource as ObservableCollection<EntryRow>;
			if (items is null) return;

			foreach (EntryRow item in items)
			{
				// this means that the item is not in the entries hashtable
				if (item.Id is null or 0)
				{
				}
				else
				{
					//Hashtable entry = entries.TakeWhere(e => e.Value is Hashtable ht && (ht["id"] as int?) == item.ID);
				}
			}

		}
		private static HackFile[]? GetHackNonEntries(TreeViewNode node, Dictionary<string, Task<HackFile>>? hackFileMap)
		{
			string path = HackDefaults.DefaultPath((node.Content as TreeData)?.FullPath, true);
			if (!Directory.Exists(path)) return null;

			HackFile[] files;

			bool hasEntries = hackFileMap != null;
			if (hasEntries) files = FileOperations.FilesInDirectory(path, hackFileMap); //, out Dictionary<string, Hashtable> conflictPaths);
			else files = FileOperations.FilesInDirectory(path);
			return files;
		}
		internal async void AddRemoteEntries(DataGrid grid, Hashtable entries, Dictionary<string, Task<HackFile>> hackFileMap)
		{
#if DEBUG
			_HFM._stopwatch = Stopwatch.StartNew();
#endif
			foreach (DictionaryEntry pair in entries)
			{
				await AddRemoteEntry(grid, pair, hackFileMap);
			}
#if DEBUG
			_HFM._stopwatch.Stop();
			Console.WriteLine($"remote entries time: {_HFM._stopwatch.Elapsed}");
#endif
		}
		private static async Task AddRemoteEntry(DataGrid grid, DictionaryEntry pair, Dictionary<string, Task<HackFile>> hackFileMap)
		{
			if (pair.Value is not Hashtable table) return;

			//ListViewItem item = EmptyListItemInternal(OdooEntryList);
			EntryRow item = new();
			item.Id = table["id"] as int?;

			//item.SubItems.Add(((int)table["id"]).ToString());
			item.Name = pair.Key.ToString();

			object ttype = table["type"];
			item.Type = ttype is string ttypeString ? ttypeString : StorageBox.EMPTY_PLACEHOLDER;

			//double size = (double)( Convert.ToDouble(table["size"]) * HackDefaults.ByteSizeMultiplier );
			item.Size = Convert.ToInt64(table["size"]);


			int? checkout = table["checkout"] as int?;
			item.Checkout = checkout is null or 0 ? null : OdooDefaults.IdToUser.TryGetValue(checkout ?? 0, out HpUser? user) ? user : null;

			// check if latest checksum
			string status = "";
			string? fullName = table["fullname"] as string;
			HackFile hack = null;
			if (!string.IsNullOrWhiteSpace(fullName)) hack = hackFileMap[fullName].Result;

			//string latest = EmptyPlaceholder;
			item.LatestId = table["latest"] as int?;
			string datePlace = table["latest_date"] is not string latest ? StorageBox.EMPTY_PLACEHOLDER : latest;

			item.RemoteDate = DateTime.TryParse(datePlace, out DateTime remoteDate) && remoteDate != default ? remoteDate : null;
			// 2006-12-15 01:43:49.623

			item.LocalDate = hack?.ModifiedDate.Year is null or 1 ? null : hack?.ModifiedDate;

			// remote only
			// local only
			// new remote version
			// checked out to me
			// checked out to other
			// ignore filter
			// no remote file type
			// local modification
			// deleted
			// destroyed

			if (table["deleted"] is bool deleted && !deleted)
			{
				if (checkout != 0)
				{
					// cm = checked out to me
					// co = checked out to other
					status = checkout == OdooDefaults.OdooId ? "cm" : "co";
				}
				else
				{
					switch (table["latest_checksum"])
					{
						case bool:
							{
								status = "lo";
								break;
							}
						case string latestChecksum:
							{
								if (hack?.Checksum == null) status = "ro";
								else if (hack.Checksum == latestChecksum) status = "ok";
								else
								{
									// either the local version is newer or the remote version is newer
									// because the checksums don't match
									status = remoteDate > hack.ModifiedDate ? "nv" : "lm";
								}
								break;
							}
						default: status = "lo"; break;
					}
				}
			}
			else
			{
				status = "dt";
			}


			// get or add image key

			string strKey = status != "ok" ? $"{item.Type}.{status}" : item.Type;
			BitmapImage? image = Assets.GetImage(strKey) as BitmapImage;

			if (image is null)
			{
				// image key not present in ilListIcons
				BitmapImage? imgExt = Assets.GetImage(item.Type) as BitmapImage;

				if (imgExt == null)
				{
					if (OdooDefaults.ExtToType.TryGetValue($".{item.Type}", out var hpType))
					{
						// get remote image
						imgExt = new();
						using var stream = new InMemoryRandomAccessStream();
						using var writer = new DataWriter(stream);
						try
						{
							byte[] imgBytes = FileOperations.ConvertFromBase64(hpType.icon);
							writer.WriteBytes(imgBytes);
							await writer.StoreAsync();
							await writer.FlushAsync();
							writer.DetachStream();

							stream.Seek(0);
							await imgExt.SetSourceAsync(stream);
						}
						catch
						{
						}
					}

					if (imgExt == null)
					{
						imgExt = Assets.GetImage("default") as BitmapImage;
					}
					//else
					//{
					//	Assets.SetImage(item.Type, imgExt);
					//}
				}

				// get status image

				if (status == "ok")
				{
					if (imgExt is null) strKey = "default";
				}
				else
				{
					BitmapImage? imgStatus = Assets.GetImage(status) as BitmapImage;

					// combine images
					if (imgExt is not null && imgStatus is not null)
					{
						Assets.SetImage(strKey, (await ImageUtils.OverlayBitmapImagesAsync(imgExt, imgStatus)));
					}
					else
					{
						strKey = "default";
					}
				}
			}
			item.Icon = Assets.GetImage(strKey) as BitmapImage;

			item.Status = Enum.Parse<FileStatus>(status, true);
			string category = table["category"] is string cat ? cat : StorageBox.EMPTY_PLACEHOLDER;
			item.Category = OdooDefaults.HpCategories.Where(c => c.name.Equals(category)).First();

			item.FullName = fullName;
			await GridHelp.UpdateListAsync(grid, item);
		}
		internal async void AddLocalEntries(DataGrid grid, TreeViewNode node, Dictionary<string, Task<HackFile>>? hackFileMap = null)
		{
#if DEBUG
			_HFM._stopwatch = Stopwatch.StartNew();
#endif

			HackFile[]? files = GetHackNonEntries(node, hackFileMap);

			if (files is null) return;
			foreach (HackFile file in files)
			{
				await AddLocalEntry(grid, node, file);
			}

#if DEBUG
			_HFM._stopwatch.Stop();
			Console.WriteLine($"local entries time: {_HFM._stopwatch.Elapsed}");
#endif
		}
		private async Task AddLocalEntry(DataGrid grid, TreeViewNode node, HackFile file)
		{
			if (file is null) return;
			string type = file.TypeExt.ToLower();
			string status = "lo";

			if (OdooDefaults.RestrictTypes & !OdooDefaults.ExtToType.TryGetValue(type, out var hpType))
			{
				status = "ft";
			}
			if (OdooDefaults.RestrictTypes & OdooDefaults.ExtToFilter.TryGetValue(type, out var filterType))
			{
				status = "if";
			}

			type = type[1..];

			EntryRow item = new();
			item.Id = null;
			item.Name = file.Name;


			item.Type = type;

			//double size =  (double)( file.FileSize * HackDefaults.ByteSizeMultiplier );
			item.Size = file.FileSize;

			item.LocalDate = file.ModifiedDate;
			item.RemoteDate = null;
			item.Status = FileStatus.Lo;


			// get or add image key
			string strKey = $"{type}.{status}";
			BitmapImage? image = Assets.GetImage(strKey) as BitmapImage;

			if (image == null)
			{
				// image key not present in ilListIcons
				BitmapImage? imgExt = Assets.GetImage(item.Type) as BitmapImage;
				if (imgExt == null)
				{
					// get remote image
					imgExt = new();
					using var stream = new InMemoryRandomAccessStream();
					using var writer = new DataWriter(stream);
					try
					{
						byte[] imgBytes = FileOperations.ConvertFromBase64(hpType?.icon ?? "");
						writer.WriteBytes(imgBytes);
						await writer.StoreAsync();
						await writer.FlushAsync();
						writer.DetachStream();

						stream.Seek(0);
						await imgExt.SetSourceAsync(stream);
					}
					catch
					{
					}

				}

				// get status image
				BitmapImage? imgStatus = Assets.GetImage(status) as BitmapImage;

				// combine images
				if (imgExt is not null && imgStatus is not null)
				{
					Assets.SetImage(strKey, (await ImageUtils.OverlayBitmapImagesAsync(imgExt, imgStatus)));
				}
				else
				{
					strKey = "default";
				}
			}
			try
			{
				item.Icon = Assets.GetImage(strKey) as BitmapImage;
			}
			catch { Debug.WriteLine("Can't load icon image"); }

			item.Checkout = null;
			HpCategory? nameCategory = OdooDefaults.ExtToCat.TryGetValue($".{type}", out var cat) ? cat : null;
			item.Category = nameCategory;
			item.FullName = file.FullPath;
			
			await GridHelp.UpdateListAsync(grid, item);
		}

		#endregion
	}
}
