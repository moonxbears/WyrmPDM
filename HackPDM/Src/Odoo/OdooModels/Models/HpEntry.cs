using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;

using HackPDM.Extensions.General;
using HackPDM.Hack;
using HackPDM.Odoo.Methods;

//using static System.Net.Mime.MediaTypeNames;


using OClient = HackPDM.Odoo.OdooClient;
// ReSharper disable InconsistentNaming

namespace HackPDM.Odoo.OdooModels.Models;

public class HpEntry : HpBaseModel<HpEntry>
{
    public string name;
    public string checkout_date;
    public bool deleted;
    public int latest_version_id;
    public int dir_id;
    public int type_id;
    public int cat_id;
    public int? checkout_user;
    public int? checkout_node;
    internal bool IsLatest { get; set; } = false;

    public HpEntry() {  }
    public HpEntry(
        string name,
        string checkoutDate = null,
        bool active = true,
        int latestVersionId = 0,
        int dirId = 0,
        int typeId = 0,
        int catId = 0,
        int checkoutUser = 0,
        int checkoutNode = 0)
    {
        this.name = name;
        this.deleted = !active;
        this.latest_version_id = latestVersionId;
        this.dir_id = dirId;
        this.type_id = typeId;
        this.cat_id = catId;
        this.checkout_node = checkoutNode;

        if (checkoutUser == 0) this.checkout_user = OdooDefaults.OdooId;
        else this.checkout_user = checkoutUser;
        if (checkoutDate == null) this.checkout_date = OdooDefaults.OdooDateFormat(DateTime.Now);
        else this.checkout_date = checkoutDate;
    }
    public static ArrayList GetLatestIDs(ArrayList ids)
    {
        const string latest = "latest_version_id";
           
        ArrayList list = OClient.Read(GetHpModel(), ids, [latest], 10000);

        return list;
    }
    public static int GetLatestID(int id)
    {
        ArrayList list = OClient.Read(GetHpModel(), [id], ["latest_version_id"], 10000);
        return list is not null and {Count: > 0 } ? ((list[0] as Hashtable)?["latest_version_id"] as ArrayList)?[0] is int latestId ? latestId : 0 : 0;
	}
	public static async Task<int> GetLatestIDAsync(int id)
	{
		ArrayList list = await OClient.ReadAsync(GetHpModel(), [id], ["latest_version_id"], 10000);
		return list is not null and {Count: > 0 } ?  ((list[0] as Hashtable)?["latest_version_id"] as ArrayList)?[0] is int latestId ? latestId : 0 : 0;
	}
	public int GetLatestID()
    {
        if (HashedValues.TryGetValue("latest_version_id", out int latestId)) return latestId;
		ArrayList list = OClient.Read(GetHpModel(), [this.Id], ["latest_version_id"], 10000);

		return list is not null and {Count: > 0 } ? ((list[0] as Hashtable)?["latest_version_id"] as ArrayList)?[0] is int id ? id : 0 : 0;
	}
	public bool CanCheckOut() => (checkout_user is null or 0) && !deleted;
    public bool CanUnCheckOut() => (checkout_user is not null) && checkout_user == OdooDefaults.OdooId;
        
    public async Task CheckOut()
    {
        if (!CanCheckOut()) return;
        checkout_user = OdooDefaults.OdooId;
        checkout_date = OdooDefaults.OdooDateFormat( DateTime.Now );
        checkout_node = OdooDefaults.MyNode.Id;

        await WriteChangedValuesAsync("checkout_user", "checkout_date", "checkout_node");
        HpVersion version = new(nodeId: checkout_node);
        version.Id = latest_version_id;
        await version.WriteChangedValuesAsync("node_id");
        if (HashedValues.TryGetValue("windows_complete_name", out object objpath) && objpath is string winpath)
        {
            string absPath = Path.Combine(HackDefaults.PwaPathAbsolute, winpath[5..]);
            FileInfo file = new(absPath);
            if (file.Exists)
            {
                file.Attributes &= ~FileAttributes.ReadOnly;
            }
        }

    }
    public async Task UnCheckOut()
    {
        if (!CanUnCheckOut()) return;
        checkout_user = null;
        checkout_date = null;
        checkout_node = null;

        await WriteChangedValuesAsync( "checkout_user", "checkout_date", "checkout_node" );
        if (HashedValues.TryGetValue("windows_complete_name", out object objpath) && objpath is string winpath)
        {
            string absPath = Path.Combine(HackDefaults.PwaPathAbsolute, winpath[5..]);
            FileInfo file = new(absPath);
            if (file.Exists)
            {
                file.Attributes |= FileAttributes.ReadOnly;
            }
        }
    }
    internal static async Task<HpEntry> CreateNew( HackFile hackFile, int dirId )
    {
        if (OdooDefaults.RestrictTypes & !OdooDefaults.ExtToType.TryGetValue( hackFile.TypeExt.ToLower(), out HpType type ) )
            return null;

        HpEntry newEntry = new()
        {
            name = hackFile.Name,
            deleted = false,
            dir_id = dirId,
        };
        if (type is not null)
        {
            newEntry.cat_id = type.cat_id;
            newEntry.type_id = type.Id;
        }
        await newEntry.CreateAsync( false );

        return newEntry.Id == 0 ? null : newEntry;
    }
    internal static async Task<ArrayList> GetEntryList(int[] entryIds, bool update = false)
    {
        ArrayList arr = await OClient.CommandAsync<ArrayList>(HpVersion.GetHpModel(), "get_recursive_dependency_entries", [entryIds.ToArrayList()], 1000000);
        return arr;
    }
    internal async Task LogicalDelete() 
    {
        deleted = true;
        await WriteChangedValuesAsync( "deleted" );
    }
    internal async Task LogicalUnDelete() 
    {
        deleted = false;
        await WriteChangedValuesAsync( "deleted" );
    }
    public override string ToString()
    {
        return name;
    }

}