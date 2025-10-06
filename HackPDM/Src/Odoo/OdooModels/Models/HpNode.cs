using System.Collections;
using HackPDM.Extensions.General;
//using static System.Net.Mime.MediaTypeNames;


using OClient = HackPDM.Odoo.OdooClient;
// ReSharper disable InconsistentNaming

namespace HackPDM.Odoo.OdooModels.Models;

public class HpNode : HpBaseModel<HpNode>
{
    public string name;

    public HpNode() { }
    public HpNode(
        string name)
    {
        this.name = name;
    }
    internal void UpdateNodeLatestVersions(int[] versionIds)
    {
        OClient.Command<ArrayList>(GetHpModel(), "update_node_latest_versions", [versionIds.ToArrayList()], 1000000);
    }
    public override string ToString() 
    {
        return name;
    }
}