using System.Collections;
using HackPDM.Extensions.General;
//using static System.Net.Mime.MediaTypeNames;


using OClient = HackPDM.Odoo.OdooClient;

namespace HackPDM.Odoo.OdooModels.Models;

public class HpNode : HpBaseModel<HpNode>
{
    public string Name;

    public HpNode() { }
    public HpNode(
        string name)
    {
        this.Name = name;
    }
    internal void UpdateNodeLatestVersions(int[] versionIds)
    {
        OClient.Command<ArrayList>(GetHpModel(), "update_node_latest_versions", [versionIds.ToArrayList()], 1000000);
    }
    public override string ToString() 
    {
        return Name;
    }
}