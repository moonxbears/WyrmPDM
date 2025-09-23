using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using HackPDM.ClientUtils;
using HackPDM.Data;
using HackPDM.Extensions.Controls;
using HackPDM.Extensions.General;
using HackPDM.Extensions.Odoo;
using HackPDM.Forms.Helper;
using HackPDM.Forms.Odoo;
using HackPDM.Forms.Settings;
using HackPDM.Hack;
using HackPDM.Helper;
using HackPDM.Odoo;
using HackPDM.Odoo.OdooModels;
using HackPDM.Odoo.OdooModels.Models;
using HackPDM.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Directory = System.IO.Directory;
using Image = System.Drawing.Image;
using OClient = HackPDM.Odoo.OdooClient;
using Path = System.IO.Path;
using static HackPDM.Forms.Helper.MessageBox;
using HackPDM.Src.ClientUtils.Types;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI.Popups;
using CommunityToolkit.WinUI.UI.Controls;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HackPDM.Forms.Hack;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HackFileManager : Page
{
	#region Declarations
	public static ObservableCollection<EntryRow> OEntries { get; internal set; }         = [];
	public static ObservableCollection<HistoryRow> OHistories { get; internal set; }     = [];
	public static ObservableCollection<ParentRow> OParents { get; internal set; }        = [];
	public static ObservableCollection<ChildrenRow> OChildren { get; internal set; }     = [];
	public static ObservableCollection<PropertiesRow> OProperties { get; internal set; } = [];
	public static ObservableCollection<VersionRow> OVersions { get; internal set; }      = [];
	public static ObservableCollection<TreeData> ONodes { get; internal set; }           = [];

	public static NotifyIcon Notify { get; } = Notifier.Notify;
	public static StatusDialog Dialog { get; set; }
	public static ConcurrentQueue<(StatusMessage, string)> QueueAsyncStatus = new();
	public static ListDetail ActiveList { get; set; }
	public static int DownloadBatchSize
	{
		get => OdooDefaults.DownloadBatchSize;
		set	=> OdooDefaults.DownloadBatchSize = value;
	}
	public static int SkipCounter { get; private set; }
	private static Task _entryListChange = default;
	private static Task _treeItemChange = default;
	private static (object? sender, SelectionChangedEventArgs? e) _queuedEntryChange = (null, null);
	private static (TreeView? sender, TreeViewSelectionChangedEventArgs? args) _queuedTreeChange = (null, null);

	private static BackgroundWorker _backgroundWorker = new()
	{
		WorkerSupportsCancellation = true
	};
	private static CancellationTokenSource _cSource = new();
	private static CancellationTokenSource _cTreeSource = new();

	private static ImageSource _previewImage = null;
	private static bool IsActive { get; set; } = false;
	private static int _processCounter;
	private static int _totalProcessed;
	private static int _maxCount;

	// if EntryPollingMs is set to less than or equal to 0 then it will not poll for changes
	public TreeViewNode? LastSelectedNode { get; set; } = null;
	public string? LastSelectedNodePath { get; set; } = null;
	public int EntryPollingMs { get; set; } = 5000;

	internal bool IsTreeLoaded { get; set; } = false;
	internal bool IsListLoaded { get; set; } = false;

	private HpDirectory _root;
	private static bool _isClosing = false;
	private string _swKey;
	private delegate void BackgroundMethodDel(object sender, DoWorkEventArgs e);
	private delegate void BackgroundCompleteDel(object sender, RunWorkerCompletedEventArgs e);

	public const string EMPTY_PLACEHOLDER = "-";

	// temp
	//public ListView OdooEntryList = new();
	#endregion

	#region TEST_VARIABLES
#if DEBUG
	Stopwatch _stopwatch;
#endif
	#endregion

	#region Initializers
	static HackFileManager() { }
	public HackFileManager()
	{
		InitializeComponent();
		InitializeEvents();
		this.SetFormTheme(StorageBox.MyTheme ?? ThemePreset.DefaultTheme);
		ResetListViews();
		OdooDirectoryTree.LostFocus += (s, e) =>
		{
			if (OdooDirectoryTree.SelectedNode is null) return;
			LastSelectedNode = OdooDirectoryTree.SelectedNode;
			LastSelectedNodePath = LastSelectedNode?.LinkedData.FullPath;
		};
		this.Unloaded += (s, e) =>
		{
			_isClosing = true;
			_cSource.Cancel();
			_cTreeSource.Cancel();
			_backgroundWorker.CancelAsync();
		};
		this.Loaded += HackFileManager_Load;
		this._root = HpBaseModel<HpDirectory>.GetRecordById(1);
	}
	private void InitializeEvents()
	{
		OdooDirectoryTree.SelectionChanged += OdooDirectoryTree_SelectionChanged;

		ONodes.CollectionChanged		+= CollectionChanged;
		OEntries.CollectionChanged		+= CollectionChanged;
		OHistories.CollectionChanged	+= CollectionChanged;
		OParents.CollectionChanged		+= CollectionChanged;
		OChildren.CollectionChanged		+= CollectionChanged;
		OProperties.CollectionChanged	+= CollectionChanged;
		OVersions.CollectionChanged		+= CollectionChanged;

		OdooEntryList.SelectionChanged	+= OdooEntryList_SelectionChanged;

        TreeAnalyze.Click               += (sender, args) => {};
		TreeCheckout.Click              += Tree_Click_Checkout;
		TreeCommit.Click                += Tree_Click_Commit;
		TreeDelete.Click                += Tree_Click_LogicalDelete;
		TreeGetLatest.Click             += Tree_Click_GetLatest;
		TreeLocalDelete.Click           += Tree_Click_LocalDelete;
		TreePermanentDelete.Click       += Tree_Click_PermanentDelete;
		TreeOpenDirectory.Click         += Tree_Click_OpenDirectory;
		TreeUndelete.Click				+= Tree_Click_Undelete;
		TreeUndoCheckout.Click          += Tree_Click_UndoCheckout;
		TreeLogicalDelete.Click         += Tree_Click_LogicalDelete;
		ListCheckout.Click              += List_Click_Checkout;
		ListCommit.Click                += List_Click_Commit;
		ListDelete.Click                += List_Click_LogicalDelete;
		ListGetLatest.Click             += List_Click_GetLatest;
		ListLocal.Click                 += List_Click_OpenLatestLocal;
		ListUndoCheckout.Click          += List_Click_UndoCheckout;
		ListPreview.Click               += List_Click_OpenLatestRemote;
		ListFileDirectory.Click         += List_Click_OpenDirectory;
		OdooHistory.SelectionChanged	+= OdooHistory_ItemSelectionChanged;
		
	}

	private async void Tree_Click_Undelete(object sender, RoutedEventArgs e)
	{
		await UnDeleteInternal();
	}

	private void CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
	{
		// TASK: implement
	}

	private void HackFileManager_Load(object sender, RoutedEventArgs e)
	{
		CreateTreeViewBackground();
	}

	private void FormLoaded(object sender, EventArgs e)
	{
	}
	private void CreateTreeViewBackground()
	{
		BackgroundWorker worker = new()
		{
			WorkerSupportsCancellation = true
		};
		worker.DoWork += new DoWorkEventHandler(LoadOdooDirectoryTree);
		worker.RunWorkerAsync();

		bool blnWorkCanceled = false;
		if (blnWorkCanceled) worker.CancelAsync();
	}
	private void LoadOdooDirectoryTree(object sender, DoWorkEventArgs e)
	{
		IsTreeLoaded = false;

		CreateTreeHash(_root);
		CreateLocalTree(OdooDirectoryTree);

		if (LastSelectedNode != null)
		{
			LastSelectedNode = OdooDirectoryTree.FindTreeNode(LastSelectedNodePath)?.Node;
		}

		SafeInvoker(OdooDirectoryTree, () =>
		{
			var tData = OdooDirectoryTree.ItemsSource as List<TreeData>;
			tData?.Sort((TreeData x, TreeData y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
			LastSelectedNode?.LinkedData.EnsureVisible(OdooDirectoryTree);
		});

		IsTreeLoaded = true;

		//SafeInvoke(OdooEntryImage, () =>
		//{
		//	//OdooEntryImage.Image = null;
		//});
	}
	private Dictionary<string, object> ConvertSubDirectories(Hashtable ht)
	{
		Dictionary<string, object> keyValues = new()
		{
			["id"] = ht["id"],
			["name"] = ht["name"],
		};


		Hashtable directories = (Hashtable)ht["directories"];

		Dictionary<string, IDictionary> children = [];
		foreach (DictionaryEntry value in directories)
		{
			if (value.Value is Hashtable childDirectory)
			{
				children.Add((string)value.Key, ConvertSubDirectories(childDirectory));
			}
		}
		keyValues["directories"] = children;
		return keyValues;
	}
	private void InitListViewInternal(DataGrid list, ListDetail rows)
		=> InitListView(list, rows);
	internal static void InitListView(DataGrid control, ListDetail rows) 
		=> SafeInvoker(control, () => control.ItemsSource = null);
	internal static void InitGridView(ItemsControl control) 
		=> SafeInvoker(control, () => control.ItemsSource = null);
	//internal static void InitListViewPercentage(ListView list, ListDetail rows)
	//{
	//	SafeInvokeGen(list, rows, (row) =>
	//	{
	//		list.ItemsSource = null;
	//		List<ColumnHeader> offsets = [];
	//		int unUsedPercentage = 100;
	//		foreach (ColumnInfo item in row.SortColumnOrder)
	//		{
	//			if (item.Width == 0)
	//			{
	//				offsets.Add(list.Columns.Add(item.Name, item.Name));
	//			}
	//			else
	//			{
	//				int use = (int)(list.Size.Width * (item.Width / 100f));
	//				list.Columns.Add(item.Name, item.Name, use);
	//				unUsedPercentage -= item.Width;
	//			}
	//		}
	//		int totalItems = offsets.Count;
	//		int distNum = unUsedPercentage / totalItems;
	//		foreach (var column in offsets)
	//		{
	//			if (distNum <= unUsedPercentage)
	//			{
	//				column.Width = (int)(list.Size.Width * (distNum / 100f));
	//				unUsedPercentage -= distNum;
	//			}
	//			else
	//			{
	//				column.Width = (int)(list.Size.Width * (unUsedPercentage / 100f)); ;
	//				unUsedPercentage = 0;
	//			}
	//		}
	//		list.ListViewItemSorter = row.SortRowOrder.Sort;
	//	});
	//}
	internal static T EmptyListItemInternal<T>(DataGrid list) where T : new()
		=> EmptyListItem<T>(list);
	internal static T EmptyGridTable<T>(DataGrid grid) where T : new() 
		=> EmptyListItem<T>(grid);
	internal static T EmptyListItem<T>(DataGrid list) where T : new()
	{
		T entry = new();
		SafeInvoker(list, () =>
		{
			if (list.ItemsSource is ObservableCollection<T> entries)
			{
				entries.Add(entry);
				list.ItemsSource = entries;
			}
			else
			{
				list.ItemsSource = new ObservableCollection<T>() { entry };
			}
		});
		return entry;
	}
	private void ResetSubListViews()
	{
		InitListViewInternal(OdooHistory, ColumnMap.HistoryRows);
		InitListViewInternal(OdooParents, ColumnMap.ParentRows);
		InitListViewInternal(OdooChildren, ColumnMap.ChildrenRows);
		InitListViewInternal(OdooProperties, ColumnMap.PropertiesRows);
		InitListViewInternal(OdooVersionInfoList, ColumnMap.VersionInfoRows);
	}
	private void ResetListViews()
	{
		InitListViewInternal(OdooEntryList, ColumnMap.RowWidths);
		InitListViewInternal(OdooHistory, ColumnMap.HistoryRows);
		InitListViewInternal(OdooParents, ColumnMap.ParentRows);
		InitListViewInternal(OdooChildren, ColumnMap.ChildrenRows);
		InitListViewInternal(OdooProperties, ColumnMap.PropertiesRows);
		InitListViewInternal(OdooVersionInfoList, ColumnMap.VersionInfoRows);
	}
	private void ClearEntryLists()
	{
		//OdooHistory.Clear();
		OdooHistory.ItemsSource = new HistoryRow[0];
		//OdooParents.Clear();
		OdooParents.ItemsSource = new ParentRow[0];
        //OdooChildren.Clear();
        OdooChildren.ItemsSource = new ChildrenRow[0];
		//OdooProperties.Clear();
		OdooProperties.ItemsSource = new PropertiesRow[0];

    }
	public void RefreshEntries()
	{
		SafeInvoker(OdooEntryList, () => OdooEntryList.UpdateLayout());
	}
	internal TreeView GetOdooDirectoryTree()
		=> OdooDirectoryTree;
	internal DataGrid GetOdooEntryList()
		=> OdooEntryList;
	#endregion

	#region TreeView functions
	// tree view directories
	private async Task<(Hashtable entries, Dictionary<string, Task<HackFile>> hackmap)> GetHackAndEntry(int? directoryId)
	{
		if (directoryId is null) return (null, null);
		Hashtable entries = await Task.Run(() => HpDirectory.GetEntries(directoryId, IsActive));
		Dictionary<string, Task<HackFile>> hackFileMap = await GetFileMap(entries);
		return (entries, hackFileMap);
	}
	private async Task TreeSelectItem(TreeViewNode node, CancellationToken token = default)
	{
		IsListLoaded = false;
		await AsyncHelper.WaitUntil(() => IsTreeLoaded, 100, -1, token);
		node.LinkedData.EnsureVisible(OdooDirectoryTree);
		InitListViewInternal(OdooEntryList, ColumnMap.RowWidths);

		try
		{
			TreeData? tData = node?.Content as TreeData;
			if (tData is not null)
			{
				if (tData?.DirectoryId is null or 0)
				{
					// add file entries to folder
					AddLocalEntries(node);
					return;
				}

				token.ThrowIfCancellationRequested();
				(Hashtable entries, Dictionary<string, Task<HackFile>> hackmap) = await GetHackAndEntry(tData.DirectoryId);
				token.ThrowIfCancellationRequested();
				AddRemoteEntries(entries, hackmap);
				AddLocalEntries(LastSelectedNode, hackmap);
				
				SafeInvoker(OdooEntryList, ()=> OdooEntryList.SortBy<EntryRow, string>(x => x.Name));
			}
			IsListLoaded = true;
		}
		catch { }
	}
	private async Task<bool> TreeItemsChangedPolling(int timeout = -1, CancellationToken token = default)
	{
		while (!token.IsCancellationRequested && EntryPollingMs > 0)
		{
			bool isLoaded = await AsyncHelper.WaitUntil(() => IsTreeLoaded && IsListLoaded, 1000, -1, token);

			if (!isLoaded || LastSelectedNode is null) continue;

			await Task.Delay(EntryPollingMs, token);
		}
		return false;
	}
	private void CreateTreeHash(HpDirectory directory)
	{
		AddDirectoriesToTree(OdooDirectoryTree, directory.GetSubdirectories(false));
	}
	private void CreateLocalTree(in TreeView treeView)
	{
		Dictionary<string, TreeViewNode> treeDict = Utils.ConvertTreeToDictionary(treeView);
		IEnumerable<string> pathways = Directory.EnumerateDirectories(HackDefaults.PwaPathAbsolute, "*", SearchOption.AllDirectories);
		pathways = Utils.FastSlice(pathways, HackDefaults.PwaPathAbsolute.Length, prependText: "root");

		foreach (string pathway in pathways)
		{
			string[] paths = pathway.Split('\\');
			(int, TreeViewNode?) validIndexNode = Utils.LastValidTreeIndex(in pathway, in paths, treeDict);
			// the last valid index does not go to the end meaning it didn't find the
			// remaining paths
			if (validIndexNode.Item1 != paths.Length - 1)
			{
				AddLocalDirectories(validIndexNode.Item2, paths.AsSpan(validIndexNode.Item1 + 1), treeDict);
			}
		}
	}
	private void AddLocalDirectories(TreeViewNode node, Span<string> pathway, Dictionary<string, TreeViewNode> treeDict)
	{
		string[] paths = pathway.ToArray();
		SafeInvoker(OdooDirectoryTree, () =>
		{
			for (int i = 0; i < paths.Length; i++)
			{
				TreeData? parentData = node?.Content as TreeData;
				TreeData newNode = new(paths[i], parentData)
				{
					Icon = Assets.GetImage("simple-folder-icon_32.gif") as BitmapImage,
					Tag = 0
				};

				TreeViewNode tNode = new()
				{
					Content = newNode
				};

				node.Children.Add(tNode);
				treeDict.Add(parentData.FullPath, node);
			}
		});

	}
	private void AddDirectoriesToTree(TreeView tree, Hashtable entries)
	{
		SafeInvoker(tree, () =>
		{
			tree.RootNodes.Clear();
			RecurseAddNodesAsync(null, entries, 0).Wait();
		});
	}
	private async Task RecurseAddNodesAsync(TreeViewNode treeNode, Hashtable node, int depth)
	{
		// add container node (directory name)
		TreeData dat = new(node["name"] as string);
		TreeViewNode treeNodeName = new();

		// if treeNode == null then it will be the root node
		if (treeNode == null)
		{
			treeNode = treeNodeName;
			SafeInvoker(OdooDirectoryTree, () => OdooDirectoryTree.RootNodes.Add(treeNode));
		}
		else
		{
			dat.Parent = treeNode.Content<TreeData>();
			treeNode.Children.Add(treeNodeName);
		}
			
		string path = HackDefaults.DefaultPath(dat.FullPath, true);
		if (!Directory.Exists(path))
		{
			dat.Icon = Assets.GetImage("folder-icon_remoteonly_32") as BitmapImage;
		}

		dat.DirectoryId = node["id"] as int?;

		// refresh to show active changes

		// add children
		var directory = node["directories"] as Hashtable;
		if (directory is null or { Count: < 1 }) return;

		foreach (DictionaryEntry pair in directory)
		{
			if (pair.Value is Hashtable childDirectory)
			{
				await RecurseAddNodesAsync(treeNodeName, childDirectory, depth + 1);
			}
		}
	}
	public void RefreshTree()
		=> SafeInvoker(OdooDirectoryTree, ()=> OdooDirectoryTree.UpdateLayout());
	public void RestartTree()
		=> CreateTreeViewBackground();
	public void RestartEntries()
	{
		SafeInvoker(OdooDirectoryTree, async () => await TreeSelectItem(LastSelectedNode));
		SafeInvoker(OdooEntryList, async () =>
		{
			await AsyncHelper.WaitUntil(() => IsListLoaded);
			if (OdooEntryList.SelectedItems.Count > 0)
			{
				(OdooEntryList.SelectedItems[0] as EntryRow)?.Item.StartBringIntoView();
			}
		});
	}
	#endregion

	#region Tree Item Selection
	private void FindUpdatedEntries(TreeViewNode node, Hashtable entries, Dictionary<string, Task<HackFile>> hackFileMap)
	{
		// things to check for:
		// 1. if the entry is in the entries hashtable
		// 2. if the entry is in the hackFileMap
		// 3. if the entry is not in the entries hashtable but is in the hackFileMap
		// 4. if the entry is not in the hackFileMap but is in the entries hashtable
		// 5. if the entry is not in either the entries hashtable or the hackFileMap
		// 6. if the entry is in both the entries hashtable and the hackFileMap but has been modified locally
		// 7. if the entry is in both the entries hashtable and the hackFileMap but has been modified remotely

		HackFile[] files = GetHackNonEntries(node, hackFileMap);
		ObservableCollection<EntryRow> items = OdooEntryList.ItemsSource as ObservableCollection<EntryRow>;
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
	private HackFile[]? GetHackNonEntries(TreeViewNode node, Dictionary<string, Task<HackFile>> hackFileMap)
	{
		string path = HackDefaults.DefaultPath((node.Content as TreeData)?.FullPath, true);
		if (!Directory.Exists(path)) return null;

		HackFile[] files;

		bool hasEntries = hackFileMap != null;
		if (hasEntries) files = FileOperations.FilesInDirectory(path, hackFileMap); //, out Dictionary<string, Hashtable> conflictPaths);
		else files = FileOperations.FilesInDirectory(path);
		return files;
	}
	private async void AddRemoteEntries(Hashtable entries, Dictionary<string, Task<HackFile>> hackFileMap)
	{
#if DEBUG
		_stopwatch = Stopwatch.StartNew();
#endif
		foreach (DictionaryEntry pair in entries)
		{
			await AddRemoteEntry(pair, hackFileMap);
		}
#if DEBUG
		_stopwatch.Stop();
		Console.WriteLine($"remote entries time: {_stopwatch.Elapsed}");
#endif
	}
	private async Task AddRemoteEntry(DictionaryEntry pair, Dictionary<string, Task<HackFile>> hackFileMap)
	{
		if (pair.Value is not Hashtable table) return;

		//ListViewItem item = EmptyListItemInternal(OdooEntryList);
		EntryRow item =	new();
		item.Id = table["id"] as int?;

		//item.SubItems.Add(((int)table["id"]).ToString());
		item.Name = pair.Key.ToString();

		object ttype = table["type"];
		item.Type = ttype is string ttypeString ? ttypeString : EMPTY_PLACEHOLDER;

		//double size = (double)( Convert.ToDouble(table["size"]) * HackDefaults.ByteSizeMultiplier );
		item.Size = Convert.ToInt64(table["size"]);


		string? checkout = table["checkout"] as string;
		item.Checkout = checkout is null or "False:False" ? null : OdooDefaults.IdToUser.TryGetValue(int.TryParse(checkout.Split(":")?[0], out int id) ? id : 0, out HpUser? user) ? user : null;

		// check if latest checksum
		string status = "";
		string? fullName = table["fullname"] as string;
		HackFile hack = null;
		if (!string.IsNullOrWhiteSpace(fullName)) hack = hackFileMap[fullName].Result; 

		//string latest = EmptyPlaceholder;
		string datePlace = table["latest_date"] is not string latest ? EMPTY_PLACEHOLDER : latest;

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
			if (checkout != EMPTY_PLACEHOLDER)
			{
				// cm = checked out to me
				// co = checked out to other
				status = checkout == $"{OdooDefaults.OdooUser}:{OdooDefaults.OdooId}" ? "cm" : "co";
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
						byte[] imgBytes = FileOperations.ConvertFromBase64(hpType.Icon);
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
		string category = table["category"] is string cat ? cat : EMPTY_PLACEHOLDER;
		item.Category = OdooDefaults.HpCategories.Where(c => c.Name.Equals(category)).First();
			
		item.FullName = fullName;

		SafeInvoker(OdooEntryList, () => OdooEntryList.AddItem(item));
	}
	private async void AddLocalEntries(TreeViewNode node, Dictionary<string, Task<HackFile>> hackFileMap = null)
	{
#if DEBUG
		_stopwatch = Stopwatch.StartNew();
#endif

		HackFile[] files = GetHackNonEntries(node, hackFileMap);

		if (files is null) return;
		foreach (HackFile file in files)
		{
			await AddLocalEntry(node, file);
		}

#if DEBUG
		_stopwatch.Stop();
		Console.WriteLine($"local entries time: {_stopwatch.Elapsed}");
#endif
	}
	private async Task AddLocalEntry(TreeViewNode node, HackFile file)
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
					byte[] imgBytes = FileOperations.ConvertFromBase64(hpType.Icon);
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

		item.Icon = Assets.GetImage(strKey) as BitmapImage;


		item.Checkout = null;
		HpCategory? nameCategory = OdooDefaults.ExtToCat.TryGetValue($".{type}", out var cat) ? cat : null;
		item.Category = nameCategory;
		item.FullName = file.FullPath;

		//OdooEntryList.Items.Add(item);
		UpdateListAsync(OdooEntryList, item);
	}

	#endregion

	#region List Item Selection
	private async Task ProcessEntrySelectionAsync(DataGridRow item, CancellationToken token)
	{
        HpVersion[] versions = [];
		List<HpVersionProperty[]> versionProperties = [];
		(HpVersion[], HpVersion[]) versionsRelation = ([], []);
	
		var entry = item.DataContext as EntryRow;
		if (entry is null) return;


		Task historyAndProperties = Task.Run(() =>
			{
				// get history list
				versions = GetVersionsForEntry(entry.Id ?? 0, ["preview_image", "file_contents"], insertedFields: ["create_uid"]);
			})
			.ContinueWith(task1 =>
			{
				if (versions != null && versions.Length > 0)
				{
					versionProperties = HpVersion.GetAllVersionProperties(versions.ToArrayListIDs());
				}
			});

		int? versionId = (int?)HpEntry.GetFieldValue(entry.Id ?? 0, "latest_version_id");
		Task parentTask = Task.Run(() =>
		{
			if (versionId != null)
			{
				//versionRels = GetRelFromVersions([versionID]);
				//versionsRelation = GetVersionsFromRelationship(versionRels);
				versionsRelation.Item1 =
					HpVersionRelationship.GetRelatedRecordsBySearch<HpVersion>([new ArrayList()
						{
							"child_id", "=", versionId
						}], "parent_id",
						excludedFields: ["preview_image", "node_id", "entry_id", "file_modify_stamp", "checksum", "file_contents"]
					);
			}
		});
		Task childTask = Task.Run(() =>
		{
			if (versionId != null)
			{
				versionsRelation.Item2 =
					HpVersionRelationship.GetRelatedRecordsBySearch<HpVersion>([new ArrayList()
						{
							"parent_id", "=", versionId
						}], "child_id",
						excludedFields: ["preview_image", "node_id", "entry_id", "file_modify_stamp", "checksum", "file_contents"]
					);
			}
		});

		await Task.WhenAll(historyAndProperties, parentTask, childTask);
		token.ThrowIfCancellationRequested();
		object lockObject = new();
		lock (lockObject)
		{
            
			//UpdateTabPageText(HistoryTab, $"History ({versions?.Length ?? 0})");
			//UpdateTabPageText(ParentTab, $"Where Used ({versionsRelation.Item1?.Length ?? 0})");
			//UpdateTabPageText(ChildTab, $"Dependents ({versionsRelation.Item2?.Length ?? 0})");

			PopulateHistory(in versions);
			PopulateProperties(in versionProperties);
			if (versionsRelation.Item1 != null && versionsRelation.Item1.Length > 0)
			{
				// Populating Where Used
				// HpVersion.SortReverseById(versionsRelation.Item1);
				PopulateParent(in versionsRelation.Item1);
			}
			if (versionsRelation.Item2 != null && versionsRelation.Item2.Length > 0)
			{
				// Populating Dependency
				// HpVersion.SortReverseById(versionsRelation.Item2);
				PopulateChildren(in versionsRelation.Item2);
			}
			if (versions?.Length == 1)
			{
				PopulateVersionInfo(versions[0]);
			}
			else if (versions?.Length > 1)
			{
				HpVersion latest = null;
				foreach (HpVersion version in versions)
				{
					if (latest == null || version.FileModifyStamp > latest.FileModifyStamp)
					{
						latest = version;
					}
				}
				if (latest != null)
				{
					PopulateVersionInfo(latest);
					PreviewImage(latest.Id);
				}
			}
		}
	}
	private async void UpdateListAsync<T>(DataGrid list, T item)
	{
		await Task.Yield();
		SafeInvoker(list, () => list.AddItem(item));
	}
	private HpVersion[] GetVersionsForEntry(int entryId, string[] excludedFields = null, string[] insertedFields = null)
	{
		HpVersion[] versions = [];
		ArrayList ids = [entryId];
		ArrayList al = OClient.Read(HpEntry.GetHpModel(), ids, ["version_ids"], 10000);
		if (al != null && al.Count > 0)
		{
			Hashtable ht = (Hashtable)al[0];
			ArrayList result = (ArrayList)ht["version_ids"];
			excludedFields ??= ["preview_image", "file_contents"];
			versions = HpVersion.GetRecordsByIds(result, excludedFields: excludedFields, insertFields: insertedFields);
		}
		return versions;
	}

	private static async Task<Dictionary<string, Task<HackFile>>> GetFileMap(Hashtable entries)
	{
		// need to check local files
		List<Task<HackFile>> hackTasks = new(entries.Count);
		Dictionary<string, Task<HackFile>> hackFileMap = new(entries.Count);

		foreach (DictionaryEntry pair in entries)
		{
			Hashtable ht = (Hashtable)pair.Value;
			string filepath = (string)((Hashtable)pair.Value)["fullname"];

			Task<HackFile> hackTask = HackBaseFile
				.GetHackFileAsync<HackFile>(HpDirectory
					.ConvertToWindowsPath(filepath, true));

			hackFileMap.Add(filepath, hackTask);
			hackTasks.Add(hackTask);
		}
		await Task.WhenAll(hackTasks);
		//Task.WaitAll(hackTasks.ToArray());
		return hackFileMap;
	}

	private void PopulateProperties(in List<HpVersionProperty[]> allProperties)
	{
		//"Version", 50
		//"Configuration", 100
		//"Property", 140
		//"Value", 400
		//"Type", 140
		object lockObject = new();
		lock (lockObject)
		{

			if (allProperties == null) return;
			SafeInvokeGen(OdooProperties, allProperties, (allp) =>
			{
				InitListViewInternal(OdooProperties, ColumnMap.PropertiesRows);
				foreach (HpVersionProperty[] versionProperties in allp)
				{
					if (versionProperties == null || versionProperties.Length == 0) continue;

					foreach (HpVersionProperty versionProp in versionProperties)
					{
						if (versionProp == null || versionProp.Id == 0) continue;

						var item = EmptyListItemInternal<PropertiesRow>(OdooProperties);

						item.Version = versionProp.VersionId;
						item.Configuration = versionProp.SwConfigName;
						item.Name = versionProp.PropName;
						item.Property = versionProp.PropId;

						item.Type = versionProp.GetValueType();
						item.ValueData = item.Type switch
						{
							HpVersionProperty.PropertyType.Yesno => versionProp.YesnoValue,
							HpVersionProperty.PropertyType.Number => versionProp.NumberValue,
							HpVersionProperty.PropertyType.Text => versionProp.TextValue,
							HpVersionProperty.PropertyType.Date => versionProp.DateValue,
							_ => null,
						};

						OdooProperties.AddItem(item);
					}
				}
			});
		}
	}
	private void PopulateChildren(in HpVersion[] versions)
	{
		// "Version", 50
		// "Name", 600
		object lockObject = new();
		lock (lockObject)
		{

			if (versions == null) return;
			SafeInvokeGen(OdooChildren, versions, (v) =>
			{
				InitListViewInternal(OdooChildren, ColumnMap.ChildrenRows);
				foreach (HpVersion version in v)
				{
					var item = EmptyListItemInternal<ChildrenRow>(OdooChildren);
					item.Version = version.Id;
					item.Name = version.Name;
					item.BasePath = Path.Combine(/*HackDefaults.PWAPathAbsolute,*/ version.WinPathway);
					OdooChildren.AddItem(item);
				}
			});
		}
	}
	private void PopulateParent(in HpVersion[] versions)
	{
		// "Version", 50
		// "Name", 600
		object lockObject = new();
		lock (lockObject)
		{

			if (versions == null) return;
			SafeInvokeGen(OdooParents, versions, (v) =>
			{
				InitListViewInternal(OdooParents, ColumnMap.ParentRows);
				foreach (HpVersion version in v)
				{
					var item = EmptyListItemInternal<ParentRow>(OdooParents);
					item.Version = version.Id;
					item.Name = version.Name;
					item.BasePath = version.WinPathway;

					OdooParents.AddItem(item);
				}
			});
		}
	}
	private void PopulateHistory(in HpVersion[] versions)
	{
		// "Version", 50
		// "ModUser", 140
		// "ModDate", 140
		// "Size", 75
		// "RelDate", 75
		object lockObject = new();
		lock (lockObject)
		{

			if (versions == null) return;
			SafeInvokeGen(OdooHistory, versions, (v) =>
			{
				InitListViewInternal(OdooHistory, ColumnMap.HistoryRows);
				foreach (HpVersion version in v)
				{
					var item = EmptyListItemInternal<HistoryRow>(OdooHistory);

					item.Version = version.Id;
					int? moduser = null;
					if (version.HashedValues.TryGetValue("create_uid", out object obj))
					{
						if (obj is ArrayList al && al is not null)
						{
							moduser = al[0] as int?;
						}
					}
					if (moduser is not null) item.ModUser = OdooDefaults.IdToUser.TryGetValue(moduser ?? 0, out var user) ? user : null;
					item.ModDate = version.FileModifyStamp;
					item.Size = version.FileSize;
					item.RelDate = null;

					OdooHistory.AddItem(item);
				}
			});
		}
	}
	private void PopulateVersionInfo(HpVersion version)
	{
		// int CheckoutColumnIndex = OdooEntryList.Columns["Checkout"].Index;
		// item.SubItems [ CheckoutColumnIndex ].Text == ""
		object lockObject = new();
		lock (lockObject)
		{


			if (version == null) return;
			SafeInvokeGen(OdooVersionInfoList, version, (v) =>
			{
				InitListViewInternal(OdooVersionInfoList, ColumnMap.VersionInfoRows);
				var item = EmptyListItemInternal<VersionRow>(OdooVersionInfoList);

				item.Id = version.Id;
				item.Name = version.Name;
				item.Checksum = version.Checksum;
				item.FileSize = version.FileSize;
				item.DirectoryId = version.DirId;
				item.NodeId = version.NodeId;
				item.EntryId = version.EntryId;
				item.AttachmentId = version.AttachmentId;
				item.ModifyDate = version.FileModifyStamp;
				string? path = version.HashedValues != null && version.HashedValues.ContainsKey("dir_id")
					? ((version.HashedValues["dir_id"] as ArrayList)?[1].ToString())
					: "Not Found";
				item.OdooCompletePath = path;

				OdooVersionInfoList.AddItem(item);
			});
		}
	}
	private void PreviewImage(HpVersion version)
	{
		if (version.PreviewImage is null or "") return;

		byte[] previewImageBytes = Convert.FromBase64String(version.PreviewImage);
		MemoryStream ms = new(previewImageBytes)
		{
			Position = 0
		};
		OdooEntryImage.Source = Assets.GetBitmapFromBytes(previewImageBytes);
	}
	private void PreviewImage(int? hpVersionId)
	{
		const string previewImage = "preview_image";
		if (hpVersionId is null || hpVersionId == 0) return;

		HpVersion version = HpVersion.GetRecordsByIds([hpVersionId], includedFields: [previewImage]).FirstOrDefault();
		PreviewImage(version);
	}
	#endregion

	#region Background Worker functions
	private async Task Async_GetLatest((ArrayList, CancellationToken) arguements)
	{
		object lockObject = new();
		ArrayList entryIDs = arguements.Item1;
		HpVersion[] versions;

		// add status lines for entry id and upcoming versions
		lock (lockObject)
		{
			Dialog.AddStatusLine(StatusMessage.FOUND, $"{entryIDs.Count} entries");
			Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Retrieving all latest versions associated with entries...");
		}

		versions = GetLatestVersions(entryIDs, ["preview_image", "entry_id", "node_id", "file_modify_stamp", "attachment_id", "file_contents"]);

		IEnumerable<List<HpVersion>> versionBatches = Utils.BatchList(versions, DownloadBatchSize);

		_maxCount = versions.Length;
		SkipCounter = 0;
		_processCounter = 0;

		try
		{
			await ProcessDownloadsAsync(versionBatches, arguements.Item2, 5);
		}
		catch
		{
			MessageBox.Show("Cancelled Download");
		}

		Dialog.SetProgressBar(versions.Length, versions.Length);


		MessageBox.Show($"Completed!");
		RestartEntries();
	}
	private async Task Async_Commit(ValueTuple<HpEntry[], List<HackFile>> arguments)
	{
		object lockObject = new();
		// section for checking if the existing remote file already has a version with the same checksum 
		// or possibly an entry that has a newer version from that which is downloaded locally

		ConcurrentBag<HpEntry> entries = arguments.Item1.ToConcurrentBag();
		ConcurrentSet<HackFile> hackFiles = arguments.Item2;


		// testing filter hacks..
		if (entries is not null && !entries.IsEmpty)
			entries = await FilterCommitEntries(entries);
		else entries = new();

		// section for checking if hack files have a checksum that matches the fullpath
		if (hackFiles is not null && hackFiles.Count > 0)
			hackFiles = await FilterCommitHackFiles(hackFiles);
		else hackFiles = [];

		List<HpVersion> versions = new(entries.Count() + hackFiles.Count());

		while (hackFiles.TryTake(out HackFile result))
		{
			HpVersion newVersion = await OdooDefaults.ConvertHackFile(result);
			versions.Add(newVersion);
		}

		var datas = new List<(HackFile, HpEntry, HashedValueStoring)>(entries.Count);

		while (entries.TryTake(out HpEntry entry))
		{
			string entryDir = HpDirectory.ConvertToWindowsPath(entry.HashedValues["directory_complete_name"] as string, false);
			HackFile hack = HackFile.GetFromPath(Path.Combine(HackDefaults.PwaPathAbsolute, entryDir, entry.Name));
			datas.Add((hack, entry, HashedValueStoring.None));
			//HpVersion newVersion = await OdooDefaults.CreateNewVersion(hack, entry);
			//versions.Add(newVersion);
		}

		var versionBatches = Utils.BatchList(datas, DownloadBatchSize);

		_processCounter = 0;
		SkipCounter = 0;
		_maxCount = entries.Count;
		if (versionBatches.Count > 0) Dialog.AddStatusLine(StatusMessage.INFO, $"Commiting new versions to database...");
		else Dialog.AddStatusLine(StatusMessage.INFO, $"No new remote versions to commit for existing entries to the database...");
		for (int i = 0; i < versionBatches.Count; i++)
		{
			Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Commiting batch {i + 1}/{versionBatches.Count}...");

			HpVersion[] vbatch = await HpVersion.CreateAllNew([.. versionBatches[i]]);
			versions.AddRange(vbatch);

			_processCounter += versionBatches[i].Count;
			Dialog.SetProgressBar((SkipCounter + _processCounter) / 3, _maxCount);
		}

		// create new parent, child hp_version_relationship's for versions
		Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Commiting new version relationship commits to database...");
		HpVersionRelationship.Create([.. versions]);
		Dialog.SetProgressBar(2 * (_maxCount) / 3, _maxCount);

		Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Commiting new version property commits to database...");
		HpVersionProperty.Create([.. versions]);
		Dialog.SetProgressBar(_maxCount, _maxCount);

		MessageBox.Show($"Completed!");
		RestartEntries();
	}
	private async Task Async_CheckOut(HpEntry[] entries)
	{
		object lockObject = new();

		_processCounter = 0;
		SkipCounter = 0;
		_maxCount = entries.Length;
		Dialog.AddStatusLine(StatusMessage.INFO, $"{_maxCount} check outs");
		for (int i = 0; i < entries.Length; i++)
		{
			HpEntry entry = entries[i];

			lock (lockObject)
			{
				Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Checking out {entry.Name} ({entry.Id})");
			}
			await CheckOutEntry(entry);

			lock (lockObject)
			{
				_processCounter += 1;
				Dialog.SetProgressBar((SkipCounter + _processCounter), _maxCount);
			}
		}

		Dialog.SetProgressBar(_maxCount, _maxCount);
		MessageBox.Show($"Completed!");
		RestartEntries();
	}
	private async Task Async_UnCheckOut(HpEntry[] entries)
	{
		object lockObject = new();

		_processCounter = 0;
		SkipCounter = 0;
		_maxCount = entries.Length;
		Dialog.AddStatusLine(StatusMessage.INFO, $"{_maxCount} uncheck outs");
		for (int i = 0; i < entries.Length; i++)
		{
			HpEntry entry = entries[i];

			lock (lockObject)
			{
				Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Unchecking out {entry.Name} ({entry.Id})");
			}
			await UnCheckOutEntry(entry);

			lock (lockObject)
			{
				_processCounter += 1;
				Dialog.SetProgressBar((SkipCounter + _processCounter), _maxCount);
			}
		}

		Dialog.SetProgressBar(_maxCount, _maxCount);
		MessageBox.Show($"Completed!");
		RestartEntries();
	}
	private async Task Async_PermDelete(HpEntry[] entries)
	{
		ArrayList ids = entries.Select(e => e.Id).ToArrayList();
		bool vDeleted = false;

		// using DeleteEntry also deletes entries, versions, version props, version relationships, and ir attachment records
		DialogResult result = MessageBox.Show($"Are you sure you want to permanently delete {ids.Count} entries from the database?\n" +
		                                      $"This will also permanently delete all associative versions, version properties, and version relationships", "Delete Entries and Other Records?", MessageBoxType.YesNoCancel);

		if (result is not DialogResult.Yes and not DialogResult.Ok) return;

		vDeleted = await PermanentDeleteEntry(ids);

		if (vDeleted)
		{
			Dialog.AddStatusLine(StatusMessage.SUCCESS, $"Completed permanent delete");
		}
		else
		{
			MessageBox.Show("Was unable to delete entries", "Error", type:MessageBoxType.OkCancel, icon:MessageBoxIcon.Error);
			return;
		}

		MessageBox.Show($"Completed!");
		RestartEntries();
	}
	private async Task Async_LogicalDelete(HpEntry[] entries)
	{
		object lockObject = new();
		foreach (var entry in entries)
		{
			lock (lockObject)
			{
				Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Setting InActive {entry.Name}: {entry.Id}");
			}
			await entry.LogicalDelete();

		}

		MessageBox.Show($"Completed!");
		RestartEntries();
	}
	private async Task Async_LogicalUnDelete(HpEntry[] entries)
	{
		object lockObject = new();
		foreach (var entry in entries)
		{
			lock (lockObject)
			{
				Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Setting Active {entry.Name}: {entry.Id}");
			}
			await entry.LogicalUnDelete();
		}

		Dialog.SetProgressBar(5, 5);
		MessageBox.Show($"Completed!");
		RestartEntries();
	}
	private async Task Async_ListItemChange(DataGridRow item, CancellationToken token)
	{
		try
		{
			await ProcessEntrySelectionAsync(item, token);
		}
		catch (Exception) { }
	}

	private bool WorkerRunner(Action<object, DoWorkEventArgs> function, string statusHeader = "Status", object arguments = null, CancellationTokenSource tokenSource = default)
	{
		BackgroundWorker worker = new()
		{
			WorkerSupportsCancellation = true
		};
		worker.DoWork += new DoWorkEventHandler(function);
		worker.RunWorkerAsync(arguments);

		bool blnWorkCanceled = Dialog.ShowStatusDialog(statusHeader);
		if (blnWorkCanceled)
		{
			tokenSource.Cancel();
			worker.CancelAsync();
			return false;
		}
		return true;
	}
	private async Task<bool> AsyncRunner(Func<Task> function, string statusHeader = "Status", CancellationTokenSource tokenSource = default)
	{
		var task = Task.Run(() => function(), tokenSource.Token);
		bool blnWorkCanceled = await AsyncHelper.WaitUntil(() => Dialog.Canceled || task.IsCompleted || task.IsCanceled, 500);

		if (blnWorkCanceled)
		{
			if (Dialog.Canceled || task.IsCanceled)
			{
				tokenSource.Cancel();
				try
				{
					await task; // Await to observe cancellation
				}
				catch (OperationCanceledException)
				{
					// handles cancellation feedback
				}
				return false;
			}
		}
		await task;
		return true;
	}
	#endregion

	#region CheckOut Functions
	private IEnumerable<HpEntry> FilterCheckoutEntries(HpEntry[] entries)
	{
		foreach (HpEntry entry in entries)
		{
			if (entry.CheckoutUser == 0)
			{
				yield return entry;
			}
		}
	}
	private IEnumerable<HpEntry> FilterUnCheckoutEntries(HpEntry[] entries)
	{
		foreach (HpEntry entry in entries)
		{
			if (entry.CheckoutUser != 0)
			{
				yield return entry;
			}
		}
	}
	private async Task CheckOutEntry(HpEntry entry)
	{
		if (entry == null)
			return;

		await entry.CheckOut();
	}
	private async Task UnCheckOutEntry(HpEntry entry)
	{
		if (entry == null)
			return;

		await entry.UnCheckOut();
	}
	#endregion

	#region Commit Functions
	private async Task<ConcurrentBag<HpEntry>> FilterCommitEntries(ConcurrentBag<HpEntry> entries)
	{
		if (entries == null || entries.Count < 1) return null;

		string[] excludedFields = ["preview_image", "attachment_id", "file_modify_stamp", "file_size", "node_id", "file_contents"];
		ConcurrentBag<Task<HpEntry>> tasks = [];
		object lockObject = new();

		while (entries.TryTake(out HpEntry entry))
		{
			Task<HpEntry?> entryTask = Task.Run(() =>
			{
				// true means that this entry is checked out
				if (entry.CheckoutUser != OdooDefaults.OdooId)
				{
					if (entry.CheckoutUser == 0)
					{
						lock (lockObject)
						{
							Dialog.AddStatusLine(StatusMessage.ERROR, $"entry is not checked out to you: {entry.Name} ({entry.Id})");
						}
					}
					else
					{
						lock (lockObject)
						{
							string userString = OdooDefaults.IdToUser.TryGetValue(entry.CheckoutUser ?? 0, out HpUser user) ? $"{user.Name} (id: {user.Id}))" : $"(id: {entry.CheckoutUser})";
							Dialog.AddStatusLine(StatusMessage.ERROR, $"checked out to user {userString}: {entry.Name} ({entry.Id}) ");
						}
					}
					return null;
				}
				// can eventually just change this to get the list of id's available instead
				HpVersion[] entryVersions = GetVersionsForEntry(entry.Id, excludedFields);

				if (entryVersions is null || entryVersions.Length == 0) return null;
				// check if any of the versions checksums are local
				HpVersion temp = entryVersions.First();
				if (HackFile.GetLocalVersion(entryVersions, out HackFile _))
				{
					lock (lockObject)
					{
						Dialog.AddStatusLine(StatusMessage.FOUND, $"Remote {temp.Name} has matching local version");
					}

					return null;
				}
				FileInfo file = new(Path.Combine(HackDefaults.PwaPathAbsolute, temp.WinPathway, temp.Name));
				if (!file.Exists)
				{
					lock (lockObject)
					{
						Dialog.AddStatusLine(StatusMessage.ERROR, $"{temp.Name} has no local version");
					}

					return null;
				}

				lock (lockObject)
				{
					Dialog.AddStatusLine(StatusMessage.PROCESSING, $"commiting {entryVersions.First().Name}");
				}
				return entry;
			});
			await entryTask;
			tasks.Add(entryTask);
		}
		await Task.WhenAll(tasks);
		return tasks.SkipSelect(
			taskPredicate =>
			{
				return taskPredicate.Result == null;
			},
			taskSelect => taskSelect.Result).ToConcurrentBag();
	}
	private async Task<ConcurrentSet<HackFile>> FilterCommitHackFiles(ConcurrentSet<HackFile> hackFiles)
	{
		List<Task<HackFile>> tasks = [];
		object lockObject = new();
		string combinedPattern = string.Join("|", OdooDefaults.EntryFilterPatterns);
		var regex = new Regex(combinedPattern, RegexOptions.IgnoreCase);
		//string[] filePaths = hackFiles.Select(hack => hack.FullPath).ToArray();

		List<HackFile> hacks = new();
		foreach (HackFile hack in hackFiles)
		{
			regex = new Regex(combinedPattern, RegexOptions.IgnoreCase);
			if (!regex.IsMatch($".{hack.TypeExt.ToLower()}"))
			{
				hacks.Add(hack);
			}
		}
		HackFile[] files = await FileOperations.FilesNotInOdoo(hacks);
		return files;
	}
	#endregion

	#region Latest Functions
	private HpVersion[] GetLatestVersions(ArrayList entryIDs, string[] excludedFields = null)
	{
		if (excludedFields == null) excludedFields = ["preview_image", "file_contents"];
		return HpEntry.GetRelatedRecordByIds<HpVersion>(entryIDs, "latest_version_id", excludedFields);
	}
	private async Task ProcessVersionBatchAsync(List<HpVersion> batchVersions)
	{
		object lockObject = new();
		ConcurrentBag<HpVersion> processVersions = [];
		ConcurrentBag<int> unprocessedVersions = [];
		List<Task> tasks = [];


		foreach (HpVersion version in batchVersions)
		{
			bool willProcess = true;

			// ==============================================================
			// check to see if the version has a checksum and if it is the
			// same as the one locally; if not don't download
			// ==============================================================
			if (version.Checksum == null || version.Checksum.Length == 0 || version.Checksum == "False")
			{
				QueueAsyncStatus.Enqueue((StatusMessage.ERROR, $"Checksum not found for version: {version.Name}"));
				SkipCounter++;
				willProcess = false;
			}
			if (willProcess && FileOperations.SameChecksum(version, ChecksumType.Sha1))
			{

				//unprocessedVersions.Add(version.ID);
				QueueAsyncStatus.Enqueue((StatusMessage.FOUND, $"Skipping version download: {version.Name}"));
				SkipCounter++;
				willProcess = false;
			}
			// ==============================================================

			// ==============================================================
			if (willProcess)
			{
				string fileName = Path.Combine(version.WinPathway, version.Name);
				processVersions.Add(version);

				QueueAsyncStatus.Enqueue((StatusMessage.PROCESSING, $"Downloading latest version: {fileName}"));
				_processCounter++;
			}
			_totalProcessed = SkipCounter + _processCounter;
			if (_totalProcessed % 25 == 0 || _totalProcessed >= _maxCount)
			{
				Dialog.AddStatusLines(QueueAsyncStatus);
			}
			Dialog.SetProgressBar(SkipCounter + _processCounter, _maxCount);


			//          tasks.Add(
			//              Task.Run(() =>
			//              {
			//                  if (version.checksum == null || version.checksum.Length == 0 || version.checksum == "False") 
			//{
			//	Interlocked.Increment(ref skipCounter);
			//	return null;
			//}
			//                  if (FileOperations.SameChecksum(version, ChecksumType.SHA1))
			//                  {
			//                      //unprocessedVersions.Add(version.ID);
			//                      queueAsyncStatus.Enqueue(["INFO", $"Skipping download (Found): {version.name}"]);
			//                      Interlocked.Increment(ref skipCounter);
			//                      return null;
			//                  }
			//                  return version;
			//              })
			//              .ContinueWith((task) =>
			//              {
			//                  if (task.Result == null) return;

			//                  string fileName = Path.Combine(task.Result.winPathway, task.Result.name);
			//                  processVersions.Add(task.Result);

			//                  queueAsyncStatus.Enqueue(["INFO", $"Downloading missing latest file: {fileName}"]);
			//                  Interlocked.Increment(ref processCounter);
			//              })
			//              .ContinueWith((task2) =>
			//              {
			//                  lock (lockObject)
			//                  {
			//                      if (SkipCounter % 100 == 0 || SkipCounter == maxCount)
			//                      {
			//                          Dialog.AddStatusLines(queueAsyncStatus);
			//                      }
			//                      Dialog.SetProgressBar(skipCounter + processCounter, maxCount);
			//                  }
			//              })
			//          );
		}

		await Task.Run(async () =>
		{
			if (processVersions.Count > 0)
			{
				Task<int[]> finishSuccesses = Task.WhenAll(HpVersion.BatchDownloadFiles([.. processVersions]));
				await finishSuccesses;
				return finishSuccesses.Result[0];
			}
			return 0;
		});

		//      // when all the tasks are completed for checking checksums start another task 
		//      // that then batch downloads those files to the correct folders.
		//      await Task.WhenAll(tasks)
		//.ContinueWith(async (task) =>
		//{
		//    if (processVersions.Count > 0)
		//    {
		//        Task<int[]> finishSuccesses = Task.WhenAll(HpVersion.BatchDownloadFiles(processVersions.ToList()));
		//        await finishSuccesses;
		//        return finishSuccesses.Result[0];
		//    }
		//    return 0;
		//});
	}
	public async Task ProcessDownloadsAsync(IEnumerable<List<HpVersion>> versionBatches, CancellationToken cToken, int maxConcurrency = 3)
	{
		SemaphoreSlim throttler = new(maxConcurrency);
		List<Task> allTasks = [];

		foreach (var batch in versionBatches)
		{
			await throttler.WaitAsync();

			Task task = Task.Run(async () =>
			{
				cToken.ThrowIfCancellationRequested();
				try
				{
					await ProcessVersionBatchAsync(batch);
				}
				finally
				{
					throttler.Release();
				}
			});

			allTasks.Add(task);
		}

		await Task.WhenAll(allTasks);
	}
	private async void GetLatestFromTreeNode(bool withSubdirectories = false)
	{
		Dialog = new StatusDialog();
		await Dialog.ShowWait("Get Latest");
		object lockObject = new();

		TreeViewNode? tnCurrent = LastSelectedNode;

		if (tnCurrent == null)
		{
			MessageBox.Show("current directory doesn't exist remotely");
			return;
		}

		// directory only needs ID set to find that record's entries
		HpDirectory directory = new("temp")
		{
			Id = tnCurrent.LinkedData.DirectoryId ?? 0
		};

		lock (lockObject)
		{
			Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Retrieving all entries within directory ({directory.Id})");
		}

		ArrayList entryIDs = directory.GetDirectoryEntryIDs(withSubdirectories, ShowInactive.IsChecked ?? false);
		GetLatestInternal(entryIDs);
	}
	#endregion

	#region Form Event Handlers
	// after select events
	private async void OdooDirectoryTree_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
	{
		_queuedTreeChange = (null, null);
		if (_treeItemChange is not null and { IsCompleted: false })
		{
			_queuedTreeChange = (sender, args);
			return;
		}

		if (_treeItemChange is null or { IsCompleted: true })
		{
			_cSource.Cancel();
			_cTreeSource = new();
			// Store the currently selected node
			LastSelectedNode = args.AddedItems.First() as TreeViewNode;
			LastSelectedNodePath = LastSelectedNode?.LinkedData.FullPath;
			_treeItemChange = TreeSelectItem(LastSelectedNode, _cTreeSource.Token);
			await _treeItemChange;
			if (_queuedTreeChange.sender != null && _queuedTreeChange.args != null)
			{
				OdooDirectoryTree_SelectionChanged(sender, args);
			}
		}
	}
	// item selection change events
	private async void OdooEntryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (OdooEntryList.SelectedItems.Count > 1)
			return;
		_queuedEntryChange = (null, null);
		if (_entryListChange is not null and { IsCompleted: false })
		{
			_queuedEntryChange = (sender, e);
			return;
		}
		ClearEntryLists();
		if (OdooEntryList.SelectedItems.Count == 0)
			return;

		OdooEntryImage.Source = null;
		if (_entryListChange is not (null or { IsCompleted: true })) return;
		
		_cSource = new();
		var listViewItem = (e.AddedItems.First() as EntryRow)?.Item;
		if (listViewItem != null)
		{
			_entryListChange = Async_ListItemChange(listViewItem, _cSource.Token);
			await _entryListChange;
		}
		if (_queuedEntryChange.sender != null && _queuedEntryChange.e != null)
		{
			OdooEntryList_SelectionChanged(_queuedEntryChange.sender, _queuedEntryChange.e);
		}
	}
	private void OdooHistory_ItemSelectionChanged(object sender, SelectionChangedEventArgs e)
		=> PreviewImageSelection((e.AddedItems.First() as EntryRow)?.Item, NameConfig.HistoryVersion.Name);
	private void OdooParents_ItemSelectionChanged(object sender, SelectionChangedEventArgs e)
		=> PreviewImageSelection((e.AddedItems.First() as ParentRow)?.Item, NameConfig.ParentVersion.Name);
	private void OdooChildren_ItemSelectionChanged(object sender, SelectionChangedEventArgs e)
		=> PreviewImageSelection((e.AddedItems.First() as ChildrenRow)?.Item, NameConfig.ChildrenVersion.Name);
	// change events
	private async void CheckedChange_Event(object sender, RoutedEventArgs e)
	{
		IsActive = ShowInactive.IsChecked ?? false;
		await TreeSelectItem(LastSelectedNode);
	}
	// tree open events
	private void OdooCMSTree_Opening(object sender, CancelEventArgs e)
	{
		string pathway = LastSelectedNodePath.Length < 5 ? HackDefaults.PwaPathAbsolute : Path.Combine(HackDefaults.PwaPathAbsolute, LastSelectedNodePath[5..]);
		if (Directory.Exists(pathway))
		{
			// TreeOpenDirectory.Enabled = true;
			// TreeLocalDelete.Enabled = true;
		}
		else
		{
			// TreeOpenDirectory.Enabled = false;
			// TreeLocalDelete.Enabled = false;
		}
	}
	// click events
	private void List_ColumnClick(object sender)
	{
		// ColumnInfo col = ColumnMap.RowWidths.SortRowOrder;
		// if (col.Sort is null) return;
		
		// col.Sort.IsAscending ^= true;
		// ColumnMap.RowWidths.SetActiveColumn(col);
		

		// ColumnInfo newCol = ColumnMap.RowWidths.SortColumnOrder[e.Column];
		// ColumnMap.RowWidths.SetActiveColumn(newCol);
		// ColumnMap.RowWidths.SortRowOrder = newCol;
		OdooEntryList.SortBy<EntryRow, string>(entry=>entry.Name);
		
	}

	//
	private void Tree_Click_GetLatest(object sender, RoutedEventArgs e)
		=> GetLatestFromTreeNode(true);
	private void Tree_Click_GetLatestAll(object sender, RoutedEventArgs e)
		=> Tree_Click_GetLatest(sender, e);
	private void Tree_Click_GetLatestTop(object sender, RoutedEventArgs e)
		=> GetLatestFromTreeNode(false);
	private async void Tree_Click_Commit(object sender, RoutedEventArgs e)
	{
		Dialog = new StatusDialog();

		string pathway = LastSelectedNodePath?.Length < 5 ? HackDefaults.PwaPathAbsolute : Path.Combine(HackDefaults.PwaPathAbsolute, LastSelectedNodePath?[5..] ?? "");
		HpDirectory hpDirectory;
		TreeData? dat = LastSelectedNode?.LinkedData;
		if (dat?.DirectoryId is not null and not 0)
		{
			List<string> paths = [];
			EndNodePaths(LastSelectedNode, paths);

			ArrayList splitPaths = LastSelectedNodePath.Split<ArrayList>("\\", StringSplitOptions.RemoveEmptyEntries);
			hpDirectory = (await HpDirectory.CreateNew(splitPaths)).Last();

			foreach (string path in paths)
			{
				splitPaths = path.Split<ArrayList>("\\", StringSplitOptions.RemoveEmptyEntries);
				await HpDirectory.CreateNew(splitPaths);
			}
		}
		else hpDirectory = new() { Id = dat?.DirectoryId ?? 0 };

		ArrayList entryIDs = HpDirectory.GetDirectoryEntryIDs(hpDirectory.Id, true);

		// get all files in folder path to commit.
		await CommitInternal(entryIDs, HackFile.FolderPathToHackWithDependencies(pathway));
	}
	private async void Tree_Click_Checkout(object sender, RoutedEventArgs e)
	{
		GetLatestFromTreeNode(true);
		ArrayList entryIDs = HpDirectory.GetDirectoryEntryIDs(LastSelectedNode?.LinkedData.DirectoryId ?? 0, true);
		await CheckoutInternal(entryIDs);
	}
	private async void Tree_Click_UndoCheckout(object sender, RoutedEventArgs e)
	{
		Dialog = new StatusDialog();
		ArrayList entryIDs = HpDirectory.GetDirectoryEntryIDs(LastSelectedNode?.LinkedData.DirectoryId ?? 0, true);

		await UnCheckoutInternal(entryIDs);
	}
	private void Tree_Click_OpenDirectory(object sender, RoutedEventArgs e)
	{
		string pathway = LastSelectedNodePath.Length < 5 ? HackDefaults.PwaPathAbsolute : Path.Combine(HackDefaults.PwaPathAbsolute, LastSelectedNodePath[5..]);
		if (Directory.Exists(pathway))
		{
			System.Diagnostics.Process.Start("explorer.exe", pathway);
		}
	}
	private async void Tree_Click_Restore(object sender, RoutedEventArgs e)
		=> await UnDeleteInternal(false);
	private async void Tree_Click_RestoreTop(object sender, RoutedEventArgs e)
		=> await UnDeleteInternal(false);
	private void Tree_Click_RestoreAll(object sender, RoutedEventArgs e)
		=> MessageBox.Show("Not Implemented Yet");
	private void Tree_Click_LocalDelete(object sender, RoutedEventArgs e)
	{
		string pathway = LastSelectedNodePath.Length < 5 ? HackDefaults.PwaPathAbsolute : Path.Combine(HackDefaults.PwaPathAbsolute, LastSelectedNodePath[5..]);
		DirectoryInfo directory = new(pathway);
		if (directory.Exists)
		{
			if (MessageBox.Show($"Are you sure you want to delete this directory and ({directory.EnumerateFiles().Count()}) files inside?",
				    "Delete Directory",
				    type: MessageBoxType.YesNoCancel,
				    icon: MessageBoxIcon.Warning) == DialogResult.Yes)
			{
				directory.Delete(true);
			}
		}
	}
	private async void Tree_Click_LogicalDelete(object sender, RoutedEventArgs e)
	{
		Dialog = new StatusDialog();

		ArrayList entryIDs = HpDirectory.GetDirectoryEntryIDs(LastSelectedNode?.LinkedData.DirectoryId ?? 0, true);

		await LogicalDeleteInternal(entryIDs);
	}
	private void Tree_Click_PermanentDelete(object sender, RoutedEventArgs e)
	{
#if DEBUG

#endif
	}
	//
	private async void List_Click_GetLatest(object sender, RoutedEventArgs e)
	{
		Dialog = new StatusDialog();
		await Dialog.ShowWait("Get Latest");
		var entryItem = OdooEntryList.SelectedItems;

		ArrayList entryIDs = [];

		foreach (EntryRow item in entryItem)
		{
			if (item.Id is not null)
			{
				entryIDs.Add(item.Id);
			}
		}

		GetLatestInternal(entryIDs);
	}
	private async void List_Click_Commit(object sender, RoutedEventArgs e)
	{
		Dialog = new StatusDialog();

		var entryItem = OdooEntryList.SelectedItems as List<EntryRow>;
		var locals = entryItem?.Where(e=>e.IsOnlyLocal);
		var entryIDs = entryItem?.Where(e => !e.IsOnlyLocal).ToArrayList();
		HashSet<HackFile> hackFiles = [];
		//int fullNameColumnIndex = OdooEntryList.Columns["FullName"].Index;
		if (locals is not null)
		{
			foreach (var item in locals)
			{
				string? file = item.FullName;
				if (file is null) continue;
				hackFiles.AddAll(HackFile.FilePathsToHackWithDependencies(file));
			}
		}
		await CommitInternal(entryIDs, hackFiles);
	}
	private async void List_Click_Checkout(object sender, RoutedEventArgs e)
	{
		List_Click_GetLatest(null, null);
		var entryItem = OdooEntryList.SelectedItems;

		ArrayList entryIDs = new(entryItem.Count);

		foreach (EntryRow item in entryItem)
		{
			if (item is not { Checkout: null }) continue;
			entryIDs.Add(item.Id);
		}

		if (entryIDs.Count < 1) return;

		await CheckoutInternal(entryIDs);
	}
	private async void List_Click_UndoCheckout(object sender, RoutedEventArgs e)
	{
		var entryItem = OdooEntryList.SelectedItems;

		ArrayList entryIDs = new(entryItem.Count);

		foreach (EntryRow item in entryItem)
		{
			if (item is not { Checkout: null }) continue;
			entryIDs.Add(item.Id);
		}

		if (entryIDs.Count < 1) return;
		await UnCheckoutInternal(entryIDs);
	}
	private void List_Click_Open(object sender, RoutedEventArgs e)
	{
		// open local if lm, co
		// open remote if ro, dt
		foreach (EntryRow viewItem in OdooEntryList.SelectedItems)
		{
			string? path = viewItem.FullName;
			int? idStr = viewItem.Id;
			if (path is null) continue;
			if (idStr is null or 0)
			{
				OpenLocalFile(path);
				continue;
			}
			FileStatus status = viewItem.Status;
			switch (status)
			{
				case FileStatus.Ro:
				case FileStatus.Nv:
				{
					OpenRemoteFile(viewItem.Id ?? 0);
					continue;
				}

				case FileStatus.Lm:
				case FileStatus.Ok:
				case FileStatus.Co:
				case FileStatus.Ft:
				case FileStatus.If:
				case FileStatus.Cm:
				{
					OpenLocalFile(HpDirectory.ConvertToWindowsPath(path, true));
					continue;
				}

				default:
					continue;
			}

		}
	}
	private void List_Click_OpenLatestRemote(object sender, RoutedEventArgs e)
	{
		StringBuilder errors = new();
		foreach (EntryRow viewItem in OdooEntryList.SelectedItems)
		{
			if (viewItem.Id is null or 0)
			{
				errors.AppendLine($"can't open local only file remotely {viewItem.Name}");
				continue;
			}
			string? path = viewItem.FullName;
			FileStatus status = viewItem.Status;

			switch (status)
			{
				case FileStatus.Ro:
				case FileStatus.Nv:
				case FileStatus.Lm:
				case FileStatus.Ok:
				case FileStatus.Co:
				case FileStatus.Ft:
				case FileStatus.If:
				case FileStatus.Cm:
				{
					OpenRemoteFile(viewItem.Id ?? 0);
					continue;
				}

				default:
				{
					errors.AppendLine($"can't open local only file remotely {viewItem.Name}");
					continue;
				}
			}
		}
		if (errors.Length > 0) MessageBox.Show(errors.ToString());
	}
	private void List_Click_OpenLatestLocal(object sender, RoutedEventArgs e)
	{
		StringBuilder errors = new();
		foreach (EntryRow viewItem in OdooEntryList.SelectedItems)
		{
			string? path = viewItem.FullName;

			if (viewItem.Id is null or 0)
			{
				OpenLocalFile(path);
				continue;
			}

			FileStatus status = viewItem.Status;

			switch (status)
			{
				case FileStatus.Nv:
				case FileStatus.Lm:
				case FileStatus.Ok:
				case FileStatus.Co:
				case FileStatus.Ft:
				case FileStatus.If:
				case FileStatus.Cm:
				{
					OpenLocalFile(HpDirectory.ConvertToWindowsPath(path, true));
					continue;
				}

				case FileStatus.Ro:
				default:
				{
					errors.AppendLine($"can't open remote only file locally {viewItem.Name}");
					continue;
				}
			}
		}
		if (errors.Length > 0) MessageBox.Show(errors.ToString());
	}
	private void List_Click_OpenDirectory(object sender, RoutedEventArgs e)
	{
		foreach (EntryRow item in OdooEntryList.SelectedItems)
		{
			string? path = item.FullName;

			try
			{
				// remote file path
				if (item.Id is not null and not 0)
				{
					path = HpDirectory.ConvertToWindowsPath(path, true);
				}
				FileInfo file = new FileInfo(path);
				if (!file.Exists) continue;

				FileOperations.OpenFolder(file.DirectoryName);
			}
			catch
			{
				continue;
			}
			//FileOperations.OpenFile(  );
		}
	}
	private void List_Click_Restore(object sender, RoutedEventArgs e)
	{

	}
	private void List_Click_LocalDelete(object sender, RoutedEventArgs e)
	{
		string pathway = LastSelectedNodePath?.Length < 5 ? HackDefaults.PwaPathAbsolute : Path.Combine(HackDefaults.PwaPathAbsolute, LastSelectedNodePath?[5..]);
		DirectoryInfo directory = new(pathway);
		if (!directory.Exists) return;

		var sb = new StringBuilder();
		var files = new List<FileInfo>();

		OdooEntryList.SelectedItems.Cast<EntryRow>().ToList().ForEach(item =>
		{
			string filepath = Path.Combine(pathway, item?.Name ?? "");
			FileInfo file = new(filepath);
			if (file.Exists)
			{
				sb.AppendLine(file.FullName);
				files.Add(file);
			}
		});
		bool tooMany = files.Count > 10;
		string message = tooMany ? $"Are you sure you want to delete ({files.Count}) files?" : $"Are you sure you want to delete these files?\nfiles:\n{sb}";
		if (MessageBox.Show(message,
			    "Delete Directory",
			    type:MessageBoxType.YesNoCancel,
			    icon:MessageBoxIcon.Warning) == DialogResult.Yes)
		{
			files.ForEach(f => f.Delete());
		}
		RestartEntries();
	}
	private async void List_Click_LogicalDelete(object sender, RoutedEventArgs e)
	{
		Dialog = new StatusDialog();

		var entryItem = OdooEntryList.SelectedItems;
		//var directory = HackDefaults.DefaultPath(lastSelectedNode.FullPath, true);

		ArrayList entryIDs = [];
		foreach (EntryRow item in entryItem)
		{
			if (item.Id is not null and not 0)
			{
				entryIDs.Add(item.Id);
			}
		}

		await LogicalDeleteInternal(entryIDs);
	}
	private async void List_Click_PermanentDelete(object sender, RoutedEventArgs e)
	{
#if DEBUG
		Dialog = new StatusDialog();

		var entryItem = OdooEntryList.SelectedItems;

		ArrayList entryIDs = new(entryItem.Count);

		foreach (EntryRow item in entryItem)
		{
			if (item.Id is not null and not 0)
			{
				entryIDs.Add(item.Id);
			}
		}

		HpEntry[] entries = HpEntry.GetRecordsByIds(entryIDs, excludedFields: ["type_id", "cat_id", "checkout_node"]);
		if (entries is null || entries.Length == 0)
		{
			MessageBox.Show("No entries to delete");
			return;
		}

		await AsyncRunner(() => Async_PermDelete(entries), "Permanently Delete Files");
#endif
	}
	//
	private void AdditionalTools_Click_Refresh(object sender, RoutedEventArgs e)
	{
		OdooEntryImage.Source = _previewImage;
		RestartTree();
		RestartEntries();
	}
	private void AdditionalTools_Click_Search(object sender, RoutedEventArgs e)
	{
		var searchWindow = WindowHelper.CreateWindowPage(typeof(SearchOdoo));
		searchWindow.Title = "Search Files";
	}
	private void AdditionalTools_Click_ManageTypes(object sender, RoutedEventArgs e)
		=> WindowHelper.CreateWindowPage(typeof(OdooFileTypeManager)).Title = "Manage Types";
	//
	private void History_Click_Download(object sender, RoutedEventArgs e)
	{
		var version = GetVersionFromHistory();
		FileInfo file = new(Path.Combine(version.WinPathway, version.Name));
		if (FileOperations.SameChecksum(file, version.Checksum))
		{
			if (file.Exists)
			{
				var response = MessageBox.Show("File exists as a different version.\n" +
				                               "Retry:\tDownload in the Temporary Folder\n" +
				                               "Ignore:\tOverwrite the current version\n" +
				                               "Abort:\tCancel download", "File Version Conflict", type: MessageBoxType.AbortRetryIgnore, icon: MessageBoxIcon.Warning);

				switch (response)
				{
					case DialogResult.Ignore:
						version.DownloadFile(version.WinPathway);
						break;
					case DialogResult.Yes:
						version.DownloadFile(Path.GetTempPath());
						break;
				}
			}
		}
		else
		{
			version.DownloadFile(version.WinPathway);
		}
	}
	private void History_Click_TemporaryDownload(object sender, RoutedEventArgs e)
		=> DownloadHistory(true);
	private void History_Click_OverwriteDownload(object sender, RoutedEventArgs e)
		=> DownloadHistory(false);
	private void History_Click_Open(object sender, RoutedEventArgs e)
	{

	}
	private void History_Click_OverwriteOpen(object sender, RoutedEventArgs e)
		=> DownloadOpen(false);
	private void History_Click_TemporaryOpen(object sender, RoutedEventArgs e)
		=> DownloadOpen(true);
	private void History_Click_OverwriteMove(object sender, RoutedEventArgs e)
		=> LocalMoveEntry(false);
	private void History_Click_TemporaryMove(object sender, RoutedEventArgs e)
		=> LocalMoveEntry(true);
	private async void History_DoubleClick(object sender, RoutedEventArgs e)
	{
		if (OdooHistory.SelectedItems?[0] is not HistoryRow item) return;
		if (item.Version is 0) return;
		
		HpVersion version = (await HpVersion.GetRecordsByIdsAsync([item.Version])).First();
		HpEntry entry = (await HpEntry.GetRecordsByIdsAsync([version.EntryId])).First();
		ArrayList versions = await GetVersionList(item.Version);
		HashSet<int> vIds = versions.ToHashSet<int>();
		vIds.Add(version.Id);
		string vIdsText = string.Join(", ", vIds);
		string eText = entry.LatestVersionId == item.Version ? $"You are trying to download the latest version and dependencies. Continue?" : "You are trying to download a previous version and dependencies. Continue?";
		string vText = $"version:\n" +
		               $"\tName = {version.Name}\n" +
		               $"\tID = {version.Id}\n" +
		               $"\tSize = {version.FileSize}\n" +
		               $"\tChecksum = {version.Checksum}\n" +
		               $"\tAttachID = {version.AttachmentId}\n" +
		               $"\tMod Date = {version.FileModifyStamp}\n" +
		               $"\tNode ID	= {version.NodeId}\n" +
		               $"\tDir ID = {version.DirId}\n" +
		               $"\tWin DL Path = {version.WinPathway}";
		var response = MessageBox.Show($"{eText}\n this will download version ids: {vIdsText}\n{vText}", "Version Download", type: MessageBoxType.YesNoCancel);
		if (response != DialogResult.Yes) return;
		HpVersion[] downVersions = await HpVersion.GetRecordsByIdsAsync(versions);
		if (downVersions.DownloadAll(out List<HpVersion> failed)) return;
		
		ArrayList fIDs = failed.GetIDs();
		MessageBox.Show($"failed to download version ids: {string.Join(", ", fIDs.ToArray<int>())}");
	}
	private async void OdooParents_DoubleClick(object sender, RoutedEventArgs e)
	{
		if (OdooParents.SelectedItems?[0] is not ParentRow item) return;

		string? pwaPath = item.BasePath;
		string fileName = item.Name;
		await FindSearchSelectionAsync(pwaPath, fileName);
	}
	private async void OdooChildren_DoubleClick(object sender, RoutedEventArgs e)
	{
		var item = OdooChildren.SelectedItems?[0] as ChildrenRow;
		if (item is null) return;

		string? pwaPath = item.BasePath;
		string fileName = item.Name;
		await FindSearchSelectionAsync(pwaPath, fileName);
	}


	// drag events
	private void StartOverlay(DragEventArgs e)
	{
		// start overlay graphic

		FileDragGraphics(OdooEntryList, e);
	}
	private void UpdateOverlay(DragEventArgs e)
	{
		// update overlay graphic
		// FileDragGraphics(OdooEntryList, e);
	}
	private void FileDragGraphics(Control control, DragEventArgs e)
	{
		// string[] files = e.Data.GetData(DataFormats.FileDrop) as string[] ?? new string[0];
		// if (files.Length < 1) return;
		// List<FileInfo> fileInfos = [.. files.Select(f => new FileInfo(f))];
		//
		// // get graphics reset
		// Graphics g = control.CreateGraphics();
		// g.Clear(OdooEntryList.BackColor);
		//
		// // add the size of the radial gradient
		// Rectangle controlSize = control.ClientRectangle;
		// float hypot = controlSize.Size.Width * 2;
		// PointF midPoint = new((controlSize.Width / 2) + controlSize.X, (controlSize.Height / 2) + controlSize.Y);
		// Rectangle sizeBox = new(0, 0, controlSize.Width, controlSize.Height);
		//
		// // create graphics path for radial gradient
		// PointF scalePoint = ScalePoint(new PointF(e.X, e.Y), midPoint, hypot);
		//
		// using (var gPathBrush = new LinearGradientBrush(midPoint, scalePoint, Color.AliceBlue, Color.Coral))
		// {
		// 	gPathBrush.LinearColors = [Color.AliceBlue, Color.Azure, Color.DarkSlateBlue, Color.Coral];
		// 	g.FillRectangle(gPathBrush, controlSize);
		// }
		//
		//
		// // create back color 
		// Font font = new(FontFamily.GenericSansSerif, 55f, GraphicsUnit.Pixel);
		// Font fontValid = new(FontFamily.GenericSansSerif, 15f, GraphicsUnit.Pixel);
		// Font fontInvalid = new(FontFamily.GenericSansSerif, 15f, FontStyle.Strikeout, GraphicsUnit.Pixel);
		//
		// SizeF offSet = new(controlSize.Width / 5f, controlSize.Height / 5f);
		// const float imgRadius = 25f;
		//
		// RectangleF imageLayout = new(
		// 	midPoint.X - imgRadius,
		// 	midPoint.Y - imgRadius,
		// 	imgRadius * 2,
		// 	imgRadius * 2
		// );
		// RectangleF layout = new(
		// 	imageLayout.X - 50,
		// 	imageLayout.Y - 50,
		// 	400,
		// 	100
		// );
		// Rectangle layoutPixel = new(
		// 	(int)layout.X,
		// 	(int)layout.Y,
		// 	(int)layout.Width,
		// 	(int)layout.Height
		// );
		// //Rectangle dot = new(Convert.ToInt32(midPoint.X), Convert.ToInt32(midPoint.Y), 5, 5);
		// Pen pen = new Pen(new SolidBrush(Color.FromArgb(100, Color.Black)));
		//
		//
		// //g.DrawRectangle(pen, layoutPixel);
		// Image def = ListIcons.Images["default"];
		// g.DrawImage(def, imageLayout);
		// RectangleF startRect = new(32, 50, controlSize.Width * 0.4f, 32f);
		// using (var brush = new SolidBrush(Color.Black))
		// using (var brushInvalid = new SolidBrush(Color.Crimson))
		// {
		// 	g.DrawString($"{files.Length} Files", font, brush, layout);
		//
		// 	foreach (var file in fileInfos)
		// 	{
		// 		if (!file.Exists)
		// 			continue;
		//
		// 		Image img = null;
		//
		// 		if (!OdooDefaults.ExtToType.ContainsKey(file.Extension))
		// 		{
		// 			img = ListIcons.Images["delete_image_button"];
		//
		// 			g.DrawString(file.FullName, fontInvalid, brushInvalid, startRect);
		// 		}
		// 		else
		// 		{
		// 			img = ListIcons.Images[file.Extension[1..]];
		// 			if (img == null)
		// 				img = def;
		//
		// 			g.DrawString(file.FullName, fontValid, brush, startRect);
		// 		}
		// 		g.DrawImage(img, 0, startRect.Y, 32, 32);
		// 		startRect.Y += 32;
		// 	}
		// }

		//g.DrawEllipse(pen, dot);
		//dot.X = Convert.ToInt32(scalePoint.X);
		//dot.Y = Convert.ToInt32(scalePoint.Y);
		//g.DrawEllipse(pen, dot);
	}
	private async void List_DragDrop(object sender, RoutedEvent e)
	{
		// if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
		// string[] fileDrop = e.Data.GetData(DataFormats.FileDrop) as string[];
		// if (fileDrop is null or { Length: < 1 }) return;
		//
		// Dialog = new StatusDialog();
		//
		// var directory = LastSelectedNode.FullPath;
		// var winDirect = Path.Combine(HackDefaults.PwaPathAbsolute, directory[5..]);
		// List<HackFile> hackFiles = [];
		//
		// foreach (var path in fileDrop)
		// {
		// 	//if (!HackDefaults.PWAPathAbsolute.StartsWith(path)) continue;
		// 	FileInfo file = new(path);
		// 	if (!file.Exists) continue;
		// 	file = file.CopyFile(winDirect);
		//
		// 	HackFile hack = await HackFile.GetFromFileInfo(file);
		// 	string newDirectory = path[HackDefaults.PwaPathAbsolute.Length..];
		// 	hack.RelativePath = newDirectory;
		// 	if (hack != null)
		// 		hackFiles.Add(hack);
		// }
		//
		// if (hackFiles.Count < 1) return;
		// var response = MessageBox.Show($"Are you sure you want to commit ({hackFiles.Count}) files?\n" +
		//                                $"files:\n{string.Join("\n", hackFiles.Select(f => $"{f.RelativePath}\\{f.Name}"))}", "Commit Files", type: MessageBoxType.YesNoCancel, icon: MessageBoxIcon.Warning);
		// if (response == DialogResult.Yes)
		// {
		// 	await AsyncRunner(() => Async_Commit((new HpEntry[0], hackFiles)), "Commit Files");
		// }
	}
	private void List_DragEnter(object sender, DragEventArgs e)
	{
		// if (e.Data.GetDataPresent(DataFormats.FileDrop))
		// {
		// 	e.Effect = DragDropEffects.Copy;
		// 	StartOverlay(e);
		// }
		// else
		// {
		// 	e.Effect = DragDropEffects.None;
		// }
	}
	private void List_DragLeave(object sender, RoutedEvent e)
	{
		// EndOverlay();
	}
	private void List_DragOver(object sender, DragEventArgs e)
	{
		// if (e.Data.GetDataPresent(DataFormats.FileDrop))
		// {
		// 	UpdateOverlay(e);
		// }
	}
	#endregion

	#region Form Helper Functions
	// private delegate void UpdateTabPageTextDel(TabPage page, string text);
	// private delegate void SafeInvokeDelGeneric<T>(Control c, T data, Action<T> action);
	// private delegate void SafeInvokeDel(Control c, Action action);
	private static void UpdateTabPageText(TabViewItem page, string text)
	{
		page.DispatcherQueue.TryEnqueue(()=>page.Header = text);
	}

	internal static void SafeInvokeGen<T>(Control control, T data, Action<T> action)
	{
		control.DispatcherQueue.TryEnqueue(()=>action.Invoke(data));
	}

	internal static void SafeInvoker(Control control, Action action)
	{
		control.DispatcherQueue.TryEnqueue(action.Invoke);
	}
	private void OpenLocalFile(string path)
	{
		FileOperations.OpenFile(path);
	}
	private void OpenRemoteFile(int entryId)
	{
		const string latestVersion = "latest_version_id";
		HpVersion version = HpEntry.GetRelatedRecordByIds<HpVersion>([entryId], latestVersion, excludedFields: ["preview_image"]).First();
		if (version == null)
			return;

		// download version data and place into temporary folder
		version.DownloadFile(Path.GetTempPath());
		FileOperations.OpenFile(Path.Combine(version.WinPathway, version.Name));
	}
	private void PreviewImageSelection(DataGridRow? item, string nameConfigId)
	{
		switch (item?.DataContext)
		{
			case null: break;
			case EntryRow er: if (er.Id is not null) PreviewImage(er.Id); break;
			case ChildrenRow cr: PreviewImage(cr.Version); break;
			case ParentRow pr: PreviewImage(pr.Version); break;
			default: break;
		}
	}
	public async Task FindSearchSelectionAsync(string pwaPath, string fileName, string delimiter = "\\")
	{
		// first select the treeview node
		// then select the listview item
		string[] paths = pwaPath.Split([delimiter], StringSplitOptions.None);

		var nodes = OdooDirectoryTree.RootNodes;
		TreeViewNode node = nodes[0];

		try
		{
			for (int i = 0; i < paths.Length; i++)
			{
				nodes = node.Children;

				bool wasFound = false;
				foreach (TreeViewNode n in nodes)
				{
					if (n.LinkedData.Name != paths[i]) continue;
					wasFound = true;
					node = n;
					break;
				}
				if (!wasFound) throw new ArgumentException();
			}
			foreach (var treeViewNode in OdooDirectoryTree.RootNodes)
			{
				treeViewNode.IsExpanded = false;
			}
			LastSelectedNode = node;
			LastSelectedNode.LinkedData.EnsureVisible(OdooDirectoryTree);
			OdooDirectoryTree.SelectedNode = LastSelectedNode;

			while (!IsListLoaded)
			{
				await Task.Delay(100);
			}
			DataGridRow? listItem = null;
			string index = NameConfig.SearchName.Name;
			
			foreach (EntryRow rows in OEntries)
			{
				if (rows.Name == fileName)
				{
					listItem = rows.Item;
					break;
				}
			}
			if (listItem == null) throw new ArgumentException();

			OdooEntryList.SelectedItems.Contains(listItem);
			listItem.Focus(FocusState.Programmatic);
			//(listItem.Content as EntryRow)?.
			listItem.StartBringIntoView();
		}
		catch
		{
		}
	}
	private void DownloadOpen(bool toTemp = false)
	{
		var version = DownloadHistory(toTemp);
		if (version == null)
			return;

		OpenLocalFile(Path.Combine(version.WinPathway, version.Name));
	}
	private HpVersion DownloadHistory(bool toTemp = false)
	{
		var version = GetVersionFromHistory();
		if (version is null) return null;

		if (toTemp)
			version.DownloadFile(Properties.Settings.Get<string>("TemporaryPath") ?? "");
		else
			version.DownloadFile(version.WinPathway);

		return version;
	}
	private void LocalMoveEntry(bool toTemp = false)
	{
		var version = GetVersionFromHistory();
		if (version == null) return;

		string tempFilePath = Path.Combine(Properties.Settings.Get<string>("TemporaryPath") ?? "", version.Name);
		string mainFilePath = Path.Combine(version.WinPathway, version.Name);

		FileInfo fileFrom = new FileInfo(!toTemp ? tempFilePath : mainFilePath);
		FileInfo fileTo = new FileInfo(toTemp ? tempFilePath : mainFilePath);

		string message = "";
		string caption = "";
		string boolReplace = toTemp ? "temporary" : "current";

		var icon = MessageBoxIcon.None;
		// if the file doesn't exist in temporary folder, download it an place it in current path.
		if (fileFrom.Exists)
		{
			if (fileTo.Exists)
			{
				message = $"Would you like to move this version to {boolReplace} and overwrite that version?";
				caption = "Move & Overwrite";
				icon = MessageBoxIcon.Warning;
			}
			else
			{
				// temporary version file and current version file don't exist
				message = $"Would you like to move this version to {boolReplace}?";
				caption = "Move";
				icon = MessageBoxIcon.Question;
			}
			// temporary version file doesn't exist but does exist in current
			if (DialogResult.Yes == MessageBox.Show(message, caption, type: MessageBoxType.YesNoCancel, icon: icon))
			{
				fileFrom.MoveFile(fileTo.DirectoryName);
			}
		}
		else
		{
			if (fileTo.Exists)
			{
				message = $"file doesn't exist in {fileFrom.DirectoryName}.\nWould you like to download this version to {boolReplace} and overwrite that version?";
				caption = "Download & Overwrite";
				icon = MessageBoxIcon.Warning;
			}
			else
			{
				// temporary version file and current version file don't exist
				message = $"file doesn't exist in {fileFrom.DirectoryName}.\nWould you like to download this version to {boolReplace}?";
				caption = "Download";
				icon = MessageBoxIcon.Question;
			}
			// temporary version file doesn't exist but does exist in current
			if (DialogResult.Yes == MessageBox.Show(message, caption, type: MessageBoxType.YesNoCancel, icon: icon))
			{
				version.DownloadFile(fileTo.DirectoryName);
			}
		}
		RestartEntries();
	}
	private HpVersion? GetVersionFromHistory()
	{
		if (OdooHistory.SelectedItems.Count < 1)
			return null;

		HistoryRow? item = OdooHistory.SelectedItems[0] as HistoryRow;
		
		if (item?.Version is null or 0) return null;
		
		var version = HpVersion.GetRecordById(item!.Version, HpVersion.UsualExcludedFields);
		version.WinPathway = Path.Combine(HackDefaults.PwaPathAbsolute, version.WinPathway);
		return version;
	}
	private void EndNodePaths(TreeViewNode node, in List<string> paths)
	{
		if (node.Children.Count == 0)
		{
			paths.Add(node.LinkedData.FullPath ?? "");
		}
		else
		{
			foreach (TreeViewNode cNode in node.Children)
			{
				EndNodePaths(cNode, paths);
			}
		}
	}
	private async Task<ArrayList> GetEntryList(int[] entryIds, bool update = false)
	{
		ArrayList arr = await OClient.CommandAsync<ArrayList>(HpVersion.GetHpModel(), "get_recursive_dependency_entries", [entryIds.ToArrayList()], 1000000);
		return arr;
	}
	private async Task<ArrayList> GetVersionList(params int[] versionIds)
	{
		ArrayList arr = await OClient.CommandAsync<ArrayList>(HpVersion.GetHpModel(), "get_recursive_dependency_versions", [versionIds.ToArrayList()], 1000000);
		return arr;
	}
	// private PointF ScalePoint(PointF p1, PointF p2, double desiredDistance)
	// {
	// 	PointF p3 = new(
	// 		p2.X - p1.X,
	// 		p2.Y - p1.Y
	// 	);
	//
	// 	double currentDist = Math.Sqrt(p3.X * p3.X + p3.Y * p3.Y);
	// 	double scaleFactor = desiredDistance / currentDist;
	// 	p3.X = p2.X - Convert.ToSingle(scaleFactor) * p3.X;
	// 	p3.Y = p2.Y - Convert.ToSingle(scaleFactor) * p3.Y;
	//
	// 	return p3;
	// }
	private async Task<bool> PermanentDeleteVersionProperty(ArrayList ids)
	{
		if (ids is null || ids.Count < 1) return false;

		HpVersionProperty[] vProps = null;
		bool deletedVersionProps = false;

		vProps = HpVersionProperty.GetRecordsBySearch([new ArrayList() { "version_id", "in", ids }]);
		if (vProps is not null
		    && vProps.Count() > 0)
		{
			ArrayList newIds = vProps.GetIDs();
			Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Deleting version properties...");
			deletedVersionProps = await OClient.DeleteAsync(HpVersionProperty.GetHpModel(), [newIds], 100000);
			if (deletedVersionProps)
			{
				Dialog.AddStatusLine(StatusMessage.SUCCESS, $"Deleted version properties: {string.Join(", ", newIds.ToArray())}");
			}
			else
			{
				Dialog.AddStatusLine(StatusMessage.ERROR, $"Unable to delete version properties");
			}
		}
		else
		{
			deletedVersionProps = true;
			Dialog.AddStatusLine(StatusMessage.SKIP, $"No version properties to delete");
		}
#if DEBUG
		Debug.WriteLine($"version properties deleted = {deletedVersionProps}");
#endif
		return deletedVersionProps;
	}
	private async Task<bool> PermanentDeletedVersionRelationships(ArrayList ids)
	{
		if (ids is null || ids.Count < 1) return false;

		HpVersionRelationship[] vRelationsParent = null;
		HpVersionRelationship[] vRelationsChild = null;

		bool deletedVersionRelParent = false;
		bool deletedVersionRelChild = false;

		vRelationsParent = HpVersionRelationship.GetRecordsBySearch([new ArrayList() { "parent_id", "in", ids }]);
		vRelationsChild = HpVersionRelationship.GetRecordsBySearch([new ArrayList() { "child_id", "in", ids }]);

		if (vRelationsParent is not null
		    && vRelationsParent.Count() > 0)
		{
			ArrayList newIds = vRelationsParent.GetIDs();
			Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Deleting parent version relationships...");
			deletedVersionRelParent = OClient.Delete(HpVersionRelationship.GetHpModel(), [newIds], 100000);
			if (deletedVersionRelParent)
			{
				Dialog.AddStatusLine(StatusMessage.SUCCESS, $"Deleted parent version relationships: {string.Join(", ", newIds.ToArray())}");
			}
			else
			{
				Dialog.AddStatusLine(StatusMessage.ERROR, $"Unable to delete parent version relationships");
			}
		}
		else
		{
			deletedVersionRelParent = true;
			Dialog.AddStatusLine(StatusMessage.SKIP, $"No version relationship parents to delete");
		}

		if (vRelationsChild is not null
		    && vRelationsChild.Any())
		{
			ArrayList newIds = vRelationsChild.GetIDs();
			Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Deleting child version relationships...");
			deletedVersionRelChild = await OClient.DeleteAsync(HpVersionRelationship.GetHpModel(), [newIds], 100000);
			if (deletedVersionRelChild)
			{
				Dialog.AddStatusLine(StatusMessage.SUCCESS, $"Deleted child version relationships: {string.Join(", ", newIds.ToArray())}");
			}
			else
			{
				Dialog.AddStatusLine(StatusMessage.ERROR, $"Unable to delete child version relationships");
			}
		}
		else
		{
			deletedVersionRelChild = true;
			Dialog.AddStatusLine(StatusMessage.SKIP, $"No version relationship children to delete");
		}

#if DEBUG
		Debug.WriteLine($"version parents deleted = {deletedVersionRelParent}");
		Debug.WriteLine($"version child deleted = {deletedVersionRelChild}");
#endif

		return deletedVersionRelChild && deletedVersionRelParent;
	}
	private async Task<bool> PermanentDeleteEntry(ArrayList ids)
	{
		if (ids is null || ids.Count < 1) return false;

		bool deletedVersions = await PermanentDeleteVersions(ids);
		Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Deleting entries...");
		bool deletedEntries = deletedVersions && OClient.Delete(HpEntry.GetHpModel(), [ids]);
		if (deletedEntries)
		{
			Dialog.AddStatusLine(StatusMessage.SUCCESS, $"Deleted entries");
		}
		else
		{
			Dialog.AddStatusLine(StatusMessage.ERROR, $"Unable to delete entries");
		}
#if DEBUG
		Debug.WriteLine($"Entries deleted = {deletedEntries}");
#endif
		return deletedVersions && deletedEntries;
	}
	private async Task<bool> PermanentDeleteVersions(ArrayList ids)
	{
		if (ids is null || ids.Count < 1) return false;

		HpVersion[] versions = HpEntry.GetRelatedRecordByIds<HpVersion>(ids, "version_ids", includedFields: ["ID"]);
		IrAttachment[] irAttachments = null;

		ArrayList vIds = versions?.Select(v => v.Id).ToArrayList() ?? [];

		bool deletedIrAttachments = false;
		bool deletedVersions = false;
		bool deletedVersionsProps = false;
		bool deletedVersionsRel = false;

		if (vIds.Count > 0)
		{
			deletedVersionsProps = await PermanentDeleteVersionProperty(vIds);
			deletedVersionsRel = await PermanentDeletedVersionRelationships(vIds);
			irAttachments = IrAttachment.GetRecordsBySearch(
			[
				new ArrayList() { "res_id", "in", vIds },
				new ArrayList() { "res_model", "=", HpVersion.GetHpModel()},
				new ArrayList() { "res_field", "=", "file_contents"},
			]);
		}
		Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Deleting IR Attachments...");
		deletedIrAttachments = deletedVersionsProps
		                       && deletedVersionsRel
		                       && (irAttachments is null
		                           || !irAttachments.Any()
		                           || await OClient.DeleteAsync(IrAttachment.GetHpModel(), [irAttachments.GetIDs()], 100000));

		if (deletedIrAttachments)
		{
			Dialog.AddStatusLine(StatusMessage.SUCCESS, $"Deleted IR Attachments");
		}
		else
		{
			Dialog.AddStatusLine(StatusMessage.INFO, $"unable to delete IR Attachments");
		}
		Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Deleting versions...");
		deletedVersions = deletedIrAttachments
		                  && ( vIds.Count <= 0
		                       || await OClient.DeleteAsync(HpVersion.GetHpModel(), [vIds], 100000));

		if (deletedVersions)
		{
			Dialog.AddStatusLine(StatusMessage.SUCCESS, $"Deleted versions");
		}
		else
		{
			Dialog.AddStatusLine(StatusMessage.ERROR, $"Unable to delete versions");
		}

#if DEBUG
		Debug.WriteLine($"ir attachments deleted = {deletedIrAttachments}");
		Debug.WriteLine($"versions deleted = {deletedVersions}");
#endif
		return deletedIrAttachments && deletedVersions;
	}
	//
	private async void GetLatestInternal(ArrayList entryIDs)
	{
		Dialog.AddStatusLine(StatusMessage.INFO, "Finding Entry Dependencies...");
		HpEntry[] entries = await HpEntry.GetRecordsByIdsAsync(entryIDs, includedFields: ["latest_version_id"]);
		//HpEntry[] entries = HpEntry.GetRecordsByIDS(entryIDs, includedFields: ["latest_version_id"]);

		ArrayList newIds = await GetEntryList([.. entries.Select(entry => entry.LatestVersionId)]);

		newIds.AddRange(entryIDs);
		newIds = newIds.ToHashSet<int>().ToArrayList();
		CancellationTokenSource tokenSource = new();
		(ArrayList, CancellationToken) arguments = (newIds, tokenSource.Token);
		await AsyncRunner(() => Async_GetLatest(arguments), "Get Latest", tokenSource);
	}
	private async Task CommitInternal(ArrayList entryIDs, IEnumerable<HackFile> hackFiles)
	{
		HpEntry[] entries = HpEntry.GetRecordsByIds(entryIDs, includedFields: ["latest_version_id"]);

		object arguments = null;
		HpEntry[] allEntries = null;

		if (entries is not null && entries.Length > 0)
		{
			ArrayList newIds = await GetEntryList([.. entries.Select(e => e.LatestVersionId)]);
			newIds.AddRange(entryIDs);
			newIds = newIds.ToHashSet<int>().ToArrayList();
			allEntries = HpEntry.GetRecordsByIds(newIds, excludedFields: ["type_id", "cat_id", "checkout_node"], insertFields: ["directory_complete_name"]);
		}

		await AsyncRunner(() => Async_Commit((allEntries, hackFiles.ToList())), "Commit Files");
	}
	private async Task CheckoutInternal(ArrayList entryIDs)
	{
		HpEntry[] entriesTemp = HpEntry.GetRecordsByIds(entryIDs, includedFields: ["latest_version_id"]);

		ArrayList newIds = await GetEntryList([.. entriesTemp.Select(e => e.LatestVersionId)]);

		newIds.AddRange(entryIDs);
		newIds = newIds.ToHashSet<int>().ToArrayList();

		HpEntry[] entries = HpEntry.GetRecordsByIds(newIds, excludedFields: ["type_id", "cat_id"]);

		if (entries is null || entries.Length < 1) return;
		Dialog = new StatusDialog();

		entries = [.. FilterCheckoutEntries(entries)];
		await AsyncRunner(() => Async_CheckOut(entries), "Checkout Files");
	}
	private async Task UnCheckoutInternal(ArrayList entryIDs)
	{
		if (entryIDs is null or { Count: < 1 }) return;

		HpEntry[] entriesTemp = HpEntry.GetRecordsByIds(entryIDs, includedFields: ["latest_version_id"]);
		ArrayList newIds = await GetEntryList([.. entriesTemp.Select(e => e.LatestVersionId)]);

		newIds.AddRange(entryIDs);
		newIds = newIds.ToHashSet<int>().ToArrayList();

		HpEntry[] entries = HpEntry.GetRecordsByIds(newIds, excludedFields: ["type_id", "cat_id", "checkout_node"]);

		if (entries is null || entries.Length < 1)
			return;
		Dialog = new StatusDialog();

		// filter out entries that are already checked out
		entries = [.. FilterUnCheckoutEntries(entries)];

		await AsyncRunner(() => Async_UnCheckOut(entries), "UnCheckout Files");
	}
	private async Task LogicalDeleteInternal(ArrayList entryIDs)
	{
		HpEntry[] entriesTemp = HpEntry.GetRecordsByIds(entryIDs, includedFields: ["latest_version_id"]);

		ArrayList newIds = await GetEntryList([.. entriesTemp.Select(e => e.LatestVersionId)]);

		newIds.AddRange(entryIDs);
		newIds = newIds.ToHashSet<int>().ToArrayList();

		HpEntry[] entries = HpEntry.GetRecordsByIds(newIds, excludedFields: ["type_id", "cat_id", "checkout_node"]);

		await AsyncRunner(() => Async_LogicalDelete(entries), "Logically Delete Files");
	}
	private async Task UnDeleteInternal(bool withSubdirectories = false)
	{
		Dialog = new StatusDialog();

		HpEntry[] entries = HpEntry.GetRecordsByIds(null, searchFilters: [new ArrayList() { "deleted", "=", true }, new ArrayList() { "dir_id", "=", LastSelectedNode?.LinkedData.DirectoryId ?? 0 }], excludedFields: ["type_id", "cat_id", "checkout_node"]);
		await AsyncRunner(() => Async_LogicalUnDelete(entries), "Logically UnDelete Files");
	}


	#endregion
}