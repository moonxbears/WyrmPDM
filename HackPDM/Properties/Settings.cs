using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using HackPDM.Src;
using HackPDM.Src.Helper.Compatibility;

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Storage;
using Windows.Storage.Streams;

using SB = HackPDM.Src.StorageBox;

namespace HackPDM.Properties
{
    internal partial class Settings : ApplicationSettingsBase
    {
        public static ISettingsProvider Provider { get; } = GetProvider();

        private static ISettingsProvider GetProvider()
        {
            var os = Environment.OSVersion.Version;
#pragma warning disable CA1416 // Validate platform compatibility
            return os.Major >= 10
                ? new ModernSettingsProvider()
                : new LegacySettingsProvider();
#pragma warning restore CA1416 // Validate platform compatibility
        }

        public static T? Get<T>(string key, T? defaultValue = default) => Provider.Get(key, defaultValue);
        public static void Set<T>(string key, T value) => Provider.Set(key, value);
    }
    internal static class Assets
    {
		internal static readonly Dictionary<string, string> AssetMap;
		internal static StorageFolder Storage => ApplicationData.Current.LocalFolder;

        static Assets()
        {
			AssetMap = new Dictionary<string, string>
			{
				{ "delete_image_button",							$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/delete_image_button.png" },
				{ "hackpdm-icon",									$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/hackpdm-icon.ico" },
				{ "loading",										$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/loading.gif" },
				{ "LockScreenLogo.scale-200",						$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/LockScreenLogo.scale-200.png" },
				{ "SplashScreen.scale-200",							$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/SplashScreen.scale-200.png" },
				{ "Square150x150Logo.scale-200",					$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/Square150x150Logo.scale-200.png" },
				{ "Square44x44Logo.scale-200",						$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/Square44x44Logo.scale-200.png" },
				{ "Square44x44Logo.targetsize-24_altform-unplated", $"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/Square44x44Logo.targetsize-24_altform-unplated.png" },
				{ "square_empty",									$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/square_empty.png" },
				{ "StoreLogo",										$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/StoreLogo.png" },
				{ "UnknownImage",									$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/UnknownImage.png" },
				{ "Wide310x150Logo.scale-200",						$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/Wide310x150Logo.scale-200.png" },
				{ "3mf",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/3mf.png" },
				{ "ai",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/ai.png" },
				{ "asmdot",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/asmdot.png" },
				{ "asmprp",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/asmprp.png" },
				{ "avi",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/avi.png" },
				{ "bas",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/bas.png" },
				{ "bat",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/bat.png" },
				{ "bmp",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/bmp.png" },
				{ "btl",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/btl.png" },
				{ "cnc",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/cnc.png" },
				{ "cs",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/cs.png" },
				{ "csproj",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/csproj.png" },
				{ "csv",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/csv.png" },
				{ "cwr",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/cwr.png" },
				{ "dat",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/dat.png" },
				{ "db",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/db.png" },
				{ "default",										$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/default.png" },
				{ "dic",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/dic.png" },
				{ "doc",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/doc.png" },
				{ "docx",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/docx.png" },
				{ "dot",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/dot.png" },
				{ "drwdot",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/drwdot.png" },
				{ "dwg",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/dwg.png" },
				{ "dxf",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/dxf.png" },
				{ "edrw",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/edrw.png" },
				{ "eps",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/eps.png" },
				{ "gcode",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/gcode.png" },
				{ "gif",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/gif.png" },
				{ "gz",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/gz.png" },
				{ "htm",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/htm.png" },
				{ "igs",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/igs.png" },
				{ "indd",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/indd.png" },
				{ "index",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/index.png" },
				{ "jpg",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/jpg.png" },
				{ "ldb",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/ldb.png" },
				{ "log",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/log.png" },
				{ "m",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/m.png" },
				{ "mdb",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/mdb.png" },
				{ "ods",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/ods.png" },
				{ "odt",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/odt.png" },
				{ "pdf",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/pdf.png" },
				{ "png",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/png.png" },
				{ "propdesc",										$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/propdesc.png" },
				{ "prtdot",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/prtdot.png" },
				{ "prtprp",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/prtprp.png" },
				{ "ps",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/ps.png" },
				{ "resx",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/resx.png" },
				{ "rpt",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/rpt.png" },
				{ "settings",										$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/settings.png" },
				{ "sla",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sla.png" },
				{ "sldasm",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldasm.png" },
				{ "sldblk",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldblk.png" },
				{ "sldbomtbt",										$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldbomtbt.png" },
				{ "slddrt",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/slddrt.png" },
				{ "slddrw",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/slddrw.png" },
				{ "sldedb",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldedb.png" },
				{ "sldedbold",										$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldedbold.png" },
				{ "sldgtolfvt",										$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldgtolfvt.png" },
				{ "sldholtbt",										$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldholtbt.png" },
				{ "sldlfp",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldlfp.png" },
				{ "sldmtnfvt",										$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldmtnfvt.png" },
				{ "sldprt",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldprt.png" },
				{ "sldpuntbt",										$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldpuntbt.png" },
				{ "sldrevtbt",										$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldrevtbt.png" },
				{ "sldsffvt",										$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldsffvt.png" },
				{ "sldtbt",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldtbt.png" },
				{ "sldweldfvt",										$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldweldfvt.png" },
				{ "sldwldtbt",										$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sldwldtbt.png" },
				{ "sln",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sln.png" },
				{ "sqy",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sqy.png" },
				{ "suo",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/suo.png" },
				{ "svg",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/svg.png" },
				{ "swp",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/swp.png" },
				{ "sym",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/sym.png" },
				{ "tbox",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/tbox.png" },
				{ "tif",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/tif.png" },
				{ "ttf",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/ttf.png" },
				{ "txt",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/txt.png" },
				{ "wxm",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/wxm.png" },
				{ "wxmx",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/wxmx.png" },
				{ "xls",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/xls.png" },
				{ "xlsx",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/xlsx.png" },
				{ "xml",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/xml.png" },
				{ "x_b",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/x_b.png" },
				{ "x_t",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/x_t.png" },
				{ "zip",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.EXTENSIONFOLDER}/zip.png" },
				{ "folder-icon_checkedme_32",						$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.FOLDERICONS}/folder-icon_checkedme_32.gif" },
				{ "folder-icon_checkedother_32",					$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.FOLDERICONS}/folder-icon_checkedother_32.gif" },
				{ "folder-icon_deleted_32",							$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.FOLDERICONS}/folder-icon_deleted_32.gif" },
				{ "folder-icon_localonly_32",						$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.FOLDERICONS}/folder-icon_localonly_32.gif" },
				{ "folder-icon_remoteonly_32",						$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.FOLDERICONS}/folder-icon_remoteonly_32.gif" },
				{ "simple-folder-icon",								$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.FOLDERICONS}/simple-folder-icon.gif" },
				{ "simple-folder-icon_32",							$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.FOLDERICONS}/simple-folder-icon_32.gif" },
				{ "cm",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/cm.png" },
				{ "cm_640",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/cm_640.png" },
				{ "co",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/co.png" },
				{ "co_640",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/co_640.png" },
				{ "ds",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/ds.png" },
				{ "ds_640",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/ds_640.png" },
				{ "dt",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/dt.png" },
				{ "dt_640",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/dt_640.png" },
				{ "ft",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/ft.png" },
				{ "ft_640",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/ft_640.png" },
				{ "if",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/if.png" },
				{ "if_640",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/if_640.png" },
				{ "lm",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/lm.png" },
				{ "lm_640",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/lm_640.png" },
				{ "lo",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/lo.png" },
				{ "lo_640",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/lo_640.png" },
				{ "nv",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/nv.png" },
				{ "nv_640",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/nv_640.png" },
				{ "ro",												$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/ro.png" },
				{ "ro_640",											$"{SB.ASSETSPREFIX}{SB.ASSETSFOLDER}/{SB.STATUSFOLDER}/ro_640.png" }
			};
			LoadInLocalDictionary();
		}
		internal static async void LoadInLocalDictionary()
		{
			string directory = Path.Combine(SB.LOCALPREFIX, SB.ASSETSFOLDER, SB.IMAGEFOLDER);
			StorageFolder localFolder = await Storage.GetFolderAsync(directory);
			StorageFile[] files = [.. await localFolder.GetFilesAsync()];
			foreach (var file in files)
			{
				AssetMap.Add(file.Name, Path.Combine(directory, $"{file.Name}.{file.FileType.ToLower()}"));
			}
		}
		private static IImageProvider GetProvider(Dictionary<string, string>? assetmap = null)
		{
			var os = Environment.OSVersion.Version;
#pragma warning disable CA1416 // Validate platform compatibility
			return os.Major >= 10
				? new AssetsImageProvider(assetmap)
				: new ImageListProvider();
#pragma warning restore CA1416 // Validate platform compatibility
		}
		public static IImageProvider ImageProvider 
		{ 
			get
			{
				field ??= GetProvider(AssetMap);
				return field;
			}
		}
		public static ImageSource? GetImage(string key)							=> ImageProvider.GetImage(key);
		public static System.Drawing.Bitmap? GetBitmap(string key)				=> ImageProvider.GetBitmap(key);
		public static IEnumerable<string> GetAvailableKeys()					=> ImageProvider.GetAvailableKeys();
		public static void SetImage(string key, byte[] imgBytes)				=> ImageProvider.SetImage(key, imgBytes);
		public async static Task<byte[]> GetImageBytes(BitmapImage img)
		{
			var streamref = RandomAccessStreamReference.CreateFromUri(img.UriSource);
			using var stream = await streamref.OpenReadAsync();
			byte[] buffer = new byte[stream.Size];
			await stream.ReadAsync(buffer.AsBuffer(), (uint)stream.Size, InputStreamOptions.None);
			return buffer;
		}
	}
}
