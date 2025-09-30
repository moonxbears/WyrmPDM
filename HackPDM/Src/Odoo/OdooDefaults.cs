using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HackPDM.Extensions.General;
using HackPDM.Hack;
using HackPDM.Odoo.OdooModels;
using HackPDM.Odoo.OdooModels.Models;
using HackPDM.Properties;
using Meziantou.Framework.Win32;
//using static System.Net.Mime.MediaTypeNames;


using OClient = HackPDM.Odoo.OdooClient;

namespace HackPDM.Odoo;

public static class OdooDefaults
{
    #region Declarations
    // made models
    public const string HP_NODE = "hp.node";
    public const string HP_ENTRY = "hp.entry";
    public const string HP_ENTRY_NAME_FILTER = "hp.entry.name.filter";
    public const string HP_DIRECTORY = "hp.directory";
    public const string HP_CATEGORY = "hp.category";
    public const string HP_CATEGORY_PROPERTY = "hp.category.property";
    public const string HP_VERSION = "hp.version";
    public const string HP_VERSION_PROPERTY = "hp.version.property";
    public const string HP_VERSION_RELATIONSHIP = "hp.version.relationship";
    public const string HP_RELEASE = "hp.release";
    public const string HP_RELEASE_VERSION_REL = "hp.release.version.rel";
    public const string HP_SETTINGS = "hp.settings";
    public const string HP_PROPERTY = "hp.property";
    public const string HP_TYPE = "hp.type";
    // adopted models
    public const string RES_USERS = "res.users";
    public const string IR_ATTACHMENT = "ir.attachment";
    public const string IR_MODEL = "ir.model";
    // odoo name identifiers
    public const string ODOO_VERSION_KEY_NAME = "client_version";
    public const string SW_KEY_NAME = "swdocmgr_key";
    public const string RESTRICT_PROP_NAME = "restrict_properties";
    public const string RESTRICT_TYPES_NAME = "restrict_types";

    public static readonly string[] DependentExt = [".SLDPRT", ".SLDASM", ".SLDDRW"];
    public static string[] EntryFilterPatterns = [.. HpEntryNameFilters?.Select(eFilter => eFilter.NameRegex) ?? []];
    // lock asynchonous operations
    private static readonly object MLockObject = new();
    public static string? OdooDb 
    {
        get => Settings.Get<string>("OdooDb");

        set
        {
            Settings.Set("OdooDb", value);
        }
    }
    public static string? OdooAddress
    {
        get => Settings.Get<string>("OdooAddress");
        set => Settings.Set("OdooAddress", value);
    }
    public static string? OdooPort
    {
        get => Settings.Get<string>("OdooPort");
        set => Settings.Set("OdooPort", value);
    }
    public static string? OdooUrl 
    {
        get 
        {
            if (field is null or "")
            {
                string? port = Settings.Get<string>("OdooPort");
                port = port is null or "" ? "" : $":{port}";

                field = $"http://{OdooAddress}{port}";
            }
            return field;
        }

        set
        {
            field = value;
        }
    }
    public static string? OdooSwKey
    {
        get => field ??= Settings.Get<string>("SwLicenseKey");

        set
        {
            Settings.Set("SwLicenseKey", value);
            field = value;
        }
    }
    public static decimal OdooAreaFactor
    {
        get => field = Settings.Get<decimal>("AreaFactor");

        set
        {
            Settings.Set("AreaFactor", value);
            field = value;
        }
    }
    public static string? OdooCredentialTarget 
    { 
        get => field ??= Settings.Get<string?>("OdooCredentialTarget", StorageBox.DEFAULT_ODOO_CREDENTIALS) ?? StorageBox.DEFAULT_ODOO_CREDENTIALS;

        set
        {
            Settings.Set("OdooCredentialTarget", value);
            field = value;
        }
    }
    public static string? OdooUser
    {
        get 
        {
            var cm = CredentialManager.ReadCredential(OdooCredentialTarget ?? StorageBox.DEFAULT_ODOO_CREDENTIALS, CredentialType.Generic);
            field = cm?.UserName;
            return field;
        }

        set
        {
            CredentialManager.WriteCredential(OdooCredentialTarget ?? StorageBox.DEFAULT_ODOO_CREDENTIALS, value, OdooPass, CredentialPersistence.LocalMachine);
            field = value;
        }
    }
    public static string? OdooPass
    {
        get
        {
            var cm = CredentialManager.ReadCredential(OdooCredentialTarget ?? StorageBox.DEFAULT_ODOO_CREDENTIALS, CredentialType.Generic);
            field = cm?.Password;
            return field;
        }

        set
        {
            CredentialManager.WriteCredential(OdooCredentialTarget ?? StorageBox.DEFAULT_ODOO_CREDENTIALS, OdooUser, value, CredentialPersistence.LocalMachine);
            field = value;
        }
    }
    public static int? OdooId
    {
        get
        {
            try
            {
                if (field is null or 0)
                {
                    field = OClient.Login(9000);
                }
            }
            catch
            {
                field = 0;
            }
            return field;
        }

        set => field = value;
    }
    public static HpNode MyNode
    {
        get
        {
            field ??= HpNodes?.First(node => node.Name.Equals(System.Environment.MachineName));
            return field;
        }
        set
        {
            field = value;
        }
    }
    public static int DownloadBatchSize
    {
        get
        {
            if (field == 0)
            {
                field = OdooDefaults.MaxBatchSize ?? 5;
            }
            return field;
        }
        set
        {
            if (field == 0)
            {
                field = OdooDefaults.MaxBatchSize ?? 5;
            }
            field = Math.Min(OdooDefaults.MaxBatchSize ?? 5, field);
        }
    }
    public static int ConcurrencySize
    {
        get
        {
            if (field == 0)
            {
                field = OdooDefaults.MaxConcurrency ?? 2;
            }
            return field;
        }
        set
        {
            if (field == 0)
            {
                field = OdooDefaults.MaxConcurrency ?? 2;
            }
            field = Math.Min(OdooDefaults.MaxConcurrency ?? 2, field);
        }
    }
    public static int? MaxConcurrency
    {
        get
        {
            field ??= HpSettings?.First(setting => setting.Name == "max_concurrency").IntValue;
            return field;
        }
    }
    public static int? MaxBatchSize
    {
        get
        {
            field ??= HpSettings?.First(setting => setting.Name == "max_batch_size").IntValue;
            return field;
        }
    }
    // low enough number of records to get before
    public static HpSetting [] HpSettings
    {
        get
        {
            field ??= HpSetting.GetAllRecords();
            return field;
        }
        set => field = value;
    }
    public static string SwApi = HpSettings?.First(sett => sett.Name == SW_KEY_NAME).CharValue ?? "";
    public static bool RestrictProperties = HpSettings?.First(sett => sett.Name == RESTRICT_PROP_NAME).BoolValue ?? true;
    public static bool RestrictTypes = HpSettings?.First(sett => sett.Name == RESTRICT_TYPES_NAME).BoolValue ?? true;
    public static HpEntryNameFilter[] HpEntryNameFilters
    {
        get
        {
            field ??= HpEntryNameFilter.GetAllRecords();
            return field;
        } 
        set => field = value;
    }
    public static HpCategory[] HpCategories
    {
        get
        {
            field ??= HpCategory.GetAllRecords();
            return field;
        }
        set => field = value;
    }
    public static HpType[] HpTypes
    {
        get
        {
            field ??= HpType.GetAllRecords();
            return field;
        }
        set => field = value;
    }
    public static HpProperty[] HpProperties
    {
        get
        {
            field ??= HpProperty.GetAllRecords();
            return field;
        }
        set => field = value;
    }
    public static HpNode[] HpNodes
    {
        get
        {
            field ??= HpNode.GetAllRecords();
            return field;
        }
        set => field = value;
    }
    public static HpUser[] HpUsers
    {
        get
        {
            field ??= HpUser.GetAllRecords();
            return field;
        }
        set => field = value;
    }

    // dictionary mapping some field type to the HpModel
    // like extension to Type or Category
    public static Dictionary<string, HpType> ExtToType 
    { 
        get
        {
            field ??= ExtensionMapType( HpTypes );
            return field;
        }
        set
        {
            field = value;
        }
    }
    public static Dictionary<string, HpCategory> ExtToCat
    {
        get
        {
            field ??= ExtensionMapCategory( HpCategories, [ .. ExtToType.Values ] );
            return field;
        }
        set
        {
            field = value;
        }
    }
    public static Dictionary<string, HpProperty> ExtToProp
    {
        get 
        {
            if (field is null)
            {
                field = [];

                foreach (HpProperty prop in HpProperties)
                {
                    field.Add(prop.Name, prop);
                }
            }
            return field;
        }
        set => field = value;
    }
    public static Dictionary<int, HpProperty> IdToProp
    {
        get
        {
            field ??= IdMapProperty(HpProperties);
            return field;
        }
        set => field = value;
    }
    public static Dictionary<int, HpUser> IdToUser
    {
        get
        {
            field ??= IdMapUser( HpUsers );
            return field;
        }
        set => field = value;
    }
    public static Dictionary<string, HpEntryNameFilter> ExtToFilter
    {
        get
        {
            field ??= ExtensionMapFilter( HpEntryNameFilters );
            return field;
        }
        set => field = value;
    }

    private static Dictionary<string, HpEntryNameFilter> ExtensionMapFilter( HpEntryNameFilter [] hpEntryNameFilters )
    {
        Dictionary<string, HpEntryNameFilter> dict = [];

        foreach ( HpEntryNameFilter filter in hpEntryNameFilters )
        {
            dict.Add( $".{filter.NameProto}", filter );
        }
        return dict;
    }

    private static Dictionary<int, HpUser> IdMapUser( in HpUser[] hpUsers )
    {
        Dictionary<int, HpUser> dict = [];

        foreach ( HpUser user in hpUsers )
        {
            dict.Add( user.Id, user );
        }
        return dict;
    }
    #endregion

    public static string OdooDateFormat(DateTime dt)
    {
        return dt.ToString("yyyy-MM-dd HH:mm:ss");
    }
    public static Dictionary<string, HpType> ExtensionMapType( in HpType [] types )
    {
        Dictionary<string, HpType> dict = [];

        foreach ( HpType type in types )
        {
            dict.Add( $".{type.FileExt.ToLower()}", type );
        }
        return dict;
    }
    public static Dictionary<string, HpCategory> ExtensionMapCategory( in HpCategory [] categories, in HpType [] types )
    {
        Dictionary<string, HpCategory> dict = [];
        foreach ( HpType type in types )
        {
            foreach ( HpCategory category in categories )
            {
                if ( category.Id == type.CatId )
                {
                    dict.Add( $".{type.FileExt.ToLower()}", category );
                    break;
                }
            }
        }
        return dict;
    }
    public static Dictionary<int, HpProperty> IdMapProperty(in HpProperty[] props )
    {
        Dictionary<int, HpProperty> dict = [];

        foreach ( HpProperty prop in props )
        {
            dict.Add( prop.Id, prop );
        }
        return dict;
    }
    public async static Task<HpVersion> ConvertHackFile(HackFile hackFile)
    {
        Hashtable ht = [];
            
        ArrayList paths = hackFile.RelativePath.Split<ArrayList>("\\", StringSplitOptions.RemoveEmptyEntries);

        try
        {
            // create directories that don't exist in odoo
            HpDirectory[] directories = await HpDirectory.CreateNew(paths);
            HpDirectory lastDirectory = directories.Last() ?? throw new Exception($"{HpDirectory.GetHpModel()} didn't create any records");
            // create an HpEntry that doesn't exist in odoo
            HpEntry entry = await HpEntry.CreateNew(hackFile, lastDirectory.Id) ?? throw new Exception($"{HpEntry.GetHpModel()} was unable to create record");
            // create an HpVersion that doesn't exist in odoo
            HpVersion version = await CreateNewVersion(hackFile, entry) ?? throw new Exception($"{HpVersion.GetHpModel()} was unable to create record");
            return version;
        }
        catch (Exception e)
        {
            Debug.WriteLine($"{e.Message}\n{e.StackTrace}");
        }
        return null;
    }
    public async static Task<HpVersion> CreateNewVersion( HackFile hack, HpEntry entry )
    {
        try { 
            // create an HpVersion that doesn't exist in odoo
            HpVersion version = await HpVersion.CreateNew(hack, entry) ?? throw new Exception( $"{HpVersion.GetHpModel()} was unable to create new version for {entry.Name}" );
            entry.LatestVersionId = version.Id;
            return version;
        }
        catch (Exception e)
        {
            Debug.WriteLine($"{e.Message}\n{e.StackTrace}");
        }
        return null;
    }
    public static string ConvertToOdooFormat(DateTime dt)
    {
        return dt.ToString( "yyyy-MM-dd HH:mm:ss" );
    }
}

//
// All the fields in the classes below correspond to a field name in the odoo module
// so reflections can map it's values from the hashtable to the class fields like newtonsoft json
// converter converts to classes with properties that align with values from the json fields.
// changing field names will break the program unless they are mapped to the names of fields in odoo models.
//

public class HpRecord : HpBaseModel<HpRecord>
{
    public bool IsCreated { get; set; }
    public string Name { get; set; }
    public HpRecord()
    {

    }

    public static implicit operator HpRecord(bool v)
    {
        HpRecord record = new() { IsCreated = v };
        return record;
    }
}
public class HpEntryMini : HpBaseModel<HpEntry>
{
    public string Name;
    public string Type;
    public long Size;
    public string Checkout;
    public string Fullname;
    public DateTime LatestDate;
    public bool? Deleted;
    public string LatestChecksum;
    public string Category;
}