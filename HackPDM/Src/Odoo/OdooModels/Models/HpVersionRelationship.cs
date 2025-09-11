using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HackPDM.Extensions.General;
using HackPDM.Hack;


//using static System.Net.Mime.MediaTypeNames;



namespace HackPDM.Odoo.OdooModels.Models;

public class HpVersionRelationship : HpBaseModel<HpVersionRelationship>
{
    public int ParentId;
    public int ChildId;

    public HpVersionRelationship() { } 
    public HpVersionRelationship(
        int parentId = 0,
        int childId = 0)
    {
        this.ParentId = parentId;
        this.ChildId = childId;
    }
    public async static void Create(params HpVersion[] versions)
    {
        //ArrayList ids = versions.Select(v => v.ID).ToArrayList();
        //ArrayList versionFields = OClient.Read(HpVersion.GetHpModel(), ids, ["id", "file_ext"]);
        HpVersionRelationship[] hvrCreate = [];
        foreach (HpVersion version in versions)
        {
            if (version is not null && !OdooDefaults.DependentExt.Contains($".{version.FileExt.ToUpper()}")) continue;
            string pathway = version.WinPathway;
            List<string> paths = [];
            List<string[]> dependencies = HackDefaults.DocMgr.GetDependencies(pathway); // NoInterrupt: true
            if (dependencies is not null && dependencies.Count > 0)
            {
                foreach (string[] deps in dependencies)
                {
                    string path = deps[1];
                    string absolute = "";
                    var splitPath = path.Split([$"\\{HackDefaults.PwaPathRelative}\\"], StringSplitOptions.RemoveEmptyEntries);
                    if (splitPath.Length == 2)
                        absolute = Path.Combine([HackDefaults.PwaPathAbsolute, splitPath[1]]);
                    else continue;
                    paths.Add(absolute);
                }
                HpVersion[] getVersions = HpVersion.GetFromPaths(includedFields: ["name", "entry_id"], fullPaths: [.. paths]);
                hvrCreate = [.. hvrCreate, .. 
                    getVersions.Select(v => new HpVersionRelationship()
                    {
                        ParentId = version.Id,
                        ChildId = v.Id,
                    })
                ];
            }
        }
            
        if (hvrCreate.Length > 0)
        {
            await MultiCreateAsync<HpVersionRelationship>(hvrCreate.ToArrayList());
        }
    }
}