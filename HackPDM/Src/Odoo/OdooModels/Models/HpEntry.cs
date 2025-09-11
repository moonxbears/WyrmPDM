using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using HackPDM.Extensions.General;
using HackPDM.Hack;
//using static System.Net.Mime.MediaTypeNames;


using OClient = HackPDM.Odoo.OdooClient;

namespace HackPDM.Odoo.OdooModels.Models;

public class HpEntry : HpBaseModel<HpEntry>
{
    public string Name;
    public string CheckoutDate;
    public bool Deleted;
    public int LatestVersionId;
    public int DirId;
    public int TypeId;
    public int CatId;
    public int? CheckoutUser;
    public int? CheckoutNode;
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
        this.Name = name;
        this.Deleted = !active;
        this.LatestVersionId = latestVersionId;
        this.DirId = dirId;
        this.TypeId = typeId;
        this.CatId = catId;
        this.CheckoutNode = checkoutNode;

        if (checkoutUser == 0) this.CheckoutUser = OdooDefaults.OdooId;
        else this.CheckoutUser = checkoutUser;
        if (checkoutDate == null) this.CheckoutDate = OdooDefaults.OdooDateFormat(DateTime.Now);
        else this.CheckoutDate = checkoutDate;
    }
    public static ArrayList GetLatestIDs(ArrayList ids)
    {
        const string latest = "latest_version_id";
           
        ArrayList list = OClient.Read(GetHpModel(), ids, [latest], 10000);

        return list;
    }
    public ArrayList GetLatestIDs()
    {
        return GetLatestIDs([this.Id]);
    }
    public bool CanCheckOut() => (CheckoutUser is null or 0) && !Deleted;
    public bool CanUnCheckOut() => (CheckoutUser is not null) && CheckoutUser == OdooDefaults.OdooId;
        
    public async Task CheckOut()
    {
        if (!CanCheckOut()) return;
        CheckoutUser = OdooDefaults.OdooId;
        CheckoutDate = OdooDefaults.OdooDateFormat( DateTime.Now );
        CheckoutNode = OdooDefaults.MyNode.Id;

        await WriteChangedValuesAsync("checkout_user", "checkout_date", "checkout_node");
        HpVersion version = new(nodeId: CheckoutNode);
        version.Id = LatestVersionId;
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
        CheckoutUser = null;
        CheckoutDate = null;
        CheckoutNode = null;

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
            Name = hackFile.Name,
            Deleted = false,
            DirId = dirId,
        };
        if (type is not null)
        {
            newEntry.CatId = type.CatId;
            newEntry.TypeId = type.Id;
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
        Deleted = true;
        await WriteChangedValuesAsync( "deleted" );
    }
    internal async Task LogicalUnDelete() 
    {
        Deleted = false;
        await WriteChangedValuesAsync( "deleted" );
    }
    public override string ToString()
    {
        return Name;
    }

}