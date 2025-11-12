using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using HackPDM.ClientUtils;
using HackPDM.Extensions.General;
using HackPDM.Extensions.Controls;
using HackPDM.Forms.Hack;
using HackPDM.Odoo.OdooModels.Models;
using HackPDM.Src.ClientUtils.Types;

using Microsoft.UI.Xaml.Controls;

using StatusDialog = HackPDM.Forms.Settings.StatusDialog;

using MessageBox = System.Windows.Forms.MessageBox;
using HackPDM.Forms.Settings;
namespace HackPDM.Odoo.Methods;

internal static class Latest
{
	internal static async Task      Async_GetLatest             ((ArrayList, CancellationToken) arguements, bool sentFromCheckout = false)
	{
		object lockObject = new();
		ArrayList entryIDs = arguements.Item1;
		HpVersion[] versions;
		var sd = StatusData.StaticData;
		// add status lines for entry id and upcoming versions
		lock (lockObject)
		{
            HackFileManager.Dialog.AddStatusLine(StatusMessage.FOUND, $"{entryIDs.Count} entries");
            HackFileManager.Dialog.AddStatusLine(StatusMessage.PROCESSING, $"Retrieving all latest versions associated with entries...");
		}

		versions = GetLatestVersions(entryIDs, ["preview_image", "entry_id", "node_id", "file_modify_stamp", "attachment_id", "file_contents"]);

		IEnumerable<List<HpVersion>> versionBatches = Help.BatchList(versions, OdooDefaults.DownloadBatchSize);

		sd.MaxCount = versions.Length;
		sd.SkipCounter = 0;
		sd.ProcessCounter = 0;
		sd.DownloadBytes = 0;

		try
		{
			await ProcessDownloadsAsync(versionBatches, arguements.Item2);
		}
		catch
		{
			MessageBox.Show("Cancelled Download");
		}

        HackFileManager.Dialog.SetProgressBar(versions.Length, versions.Length);

		if (!sentFromCheckout)
		{
			MessageBox.Show($"Completed!");
		}
		InstanceManager.GetAPage<HackFileManager>().RestartEntries();
	}
	internal static HpVersion []	GetLatestVersions			(ArrayList entryIDs, string[] excludedFields = null)
	{
		if (excludedFields == null) excludedFields = ["preview_image", "file_contents"];
		return HpEntry.GetRelatedRecordByIds<HpVersion>(entryIDs, "latest_version_id", excludedFields);
	}
	internal static async Task		ProcessVersionBatchAsync	(List<HpVersion> batchVersions)
	{
		object lockObject = new();
		ConcurrentBag<HpVersion> processVersions = [];
		ConcurrentBag<int> unprocessedVersions = [];
		List<Task> tasks = [];
		var sd = StatusData.StaticData;

		foreach (HpVersion version in batchVersions)
		{
			bool willProcess = true;
                
			// ==============================================================
			// check to see if the version has a checksum and if it is the
			// same as the one locally; if not don't download
			// ==============================================================
			if (version.checksum == null || version.checksum.Length == 0 || version.checksum == "False")
			{
				HackFileManager.QueueAsyncStatus.Enqueue((StatusMessage.ERROR, $"Checksum not found for version: {version.name}"));
				sd.SkipCounter++;
				willProcess = false;
			}
			if (willProcess && FileOperations.SameChecksum(version, ChecksumType.Sha1))
			{

				//unprocessedVersions.Add(version.ID);
				HackFileManager.QueueAsyncStatus.Enqueue((StatusMessage.FOUND, $"Skipping version download: {version.name}"));
				sd.SkipCounter++;
				willProcess = false;
			}
			// ==============================================================

			// ==============================================================
			if (willProcess)
			{
				string fileName = Path.Combine(version.WinPathway, version.name);
				processVersions.Add(version);

				HackFileManager.QueueAsyncStatus.Enqueue((StatusMessage.PROCESSING, $"Downloading latest version: {fileName}"));
				sd.ProcessCounter++;
			}
			sd.totalProcessed = sd.SkipCounter + sd.ProcessCounter;
			if (sd.totalProcessed % 25 == 0 || sd.totalProcessed >= sd.MaxCount)
			{
                HackFileManager.Dialog.SetTotalDownloaded(StatusData.SessionDownloadBytes);
                HackFileManager.Dialog.SetDownloaded(sd.DownloadBytes);
                HackFileManager.Dialog.AddStatusLines(HackFileManager.QueueAsyncStatus);
			}
            HackFileManager.Dialog.SetProgressBar(sd.SkipCounter + sd.ProcessCounter, sd.MaxCount);


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
			//                      QueueAsyncStatus.Enqueue((StatusMessage.INFO, $"Skipping download (Found): {version.name}"]);
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

			//                  QueueAsyncStatus.Enqueue((StatusMessage.INFO, $"Downloading missing latest file: {fileName}"]);
			//                  Interlocked.Increment(ref processCounter);
			//              })
			//              .ContinueWith((task2) =>
			//              {
			//                  lock (lockObject)
			//                  {
			//                      if (SkipCounter % 100 == 0 || SkipCounter == maxCount)
			//                      {
			//                          StatusDialog.Dialog.AddStatusLines(queueAsyncStatus);
			//                      }
			//                      StatusDialog.Dialog.SetProgressBar(skipCounter + processCounter, maxCount);
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
	internal static async Task		ProcessDownloadsAsync		(IEnumerable<List<HpVersion>> versionBatches, CancellationToken cToken)
	{
		SemaphoreSlim throttler = new(OdooDefaults.ConcurrencySize);
		ConcurrentQueue<Task> tasks = new();
			
		foreach (var batch in versionBatches)
		{
			await throttler.WaitAsync(cToken);

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
			}, cToken);

			if (tasks.Count > OdooDefaults.ConcurrencySize)
			{
				tasks.TryDequeue(out Task _);
			}
			tasks.Enqueue(task);
		}

		await Task.WhenAll(tasks);
	}
	internal static async void		GetLatestFromTreeNode		(bool withSubdirectories = false, bool sentFromCheckout = false)
	{
		object lockObject = new();

		var hfm = InstanceManager.GetAPage<HackFileManager>();

        TreeViewNode? tnCurrent = hfm.LastSelectedNode;

		if ( tnCurrent == null )
		{
			MessageBox.Show( "current directory doesn't exist remotely" );
			return;
		}

		// directory only needs ID set to find that record's entries
		HpDirectory directory = new("temp")
		{
			Id = tnCurrent.LinkedData.DirectoryId ?? 0,
		};

		ArrayList entryIDs = directory.GetDirectoryEntryIDs( withSubdirectories, false);
		await GetLatestInternal(entryIDs, sentFromCheckout);
	}
	internal static async Task      GetLatestInternal           (ArrayList entryIDs, bool sentFromCheckout = false)
	{
		Notifier.CancelCheckLoop();
        HackFileManager.Dialog = new StatusDialog();
		await HackFileManager.Dialog.ShowWait("Get Latest");

        HackFileManager.Dialog.AddStatusLine(StatusMessage.INFO, "Finding Entry Dependencies...");
		HpEntry[] entries = await HpEntry.GetRecordsByIdsAsync(entryIDs, includedFields: ["latest_version_id"]);
		//HpEntry[] entries = HpEntry.GetRecordsByIDS(entryIDs, includedFields: ["latest_version_id"]);

		ArrayList newIds = await HpEntry.GetEntryList([.. entries.Select(entry => entry.latest_version_id)]);

		newIds.AddRange(entryIDs);
		newIds = newIds.ToHashSet<int>().ToArrayList();
		CancellationTokenSource tokenSource = new();
		(ArrayList, CancellationToken) arguments = (newIds, tokenSource.Token);
		await AsyncHelper.AsyncRunner(() => Async_GetLatest(arguments, sentFromCheckout), "Get Latest", tokenSource);
		Notifier.FileCheckLoop();
	}
}