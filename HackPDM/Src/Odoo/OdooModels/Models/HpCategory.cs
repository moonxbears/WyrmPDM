//using static System.Net.Mime.MediaTypeNames;

namespace HackPDM.Odoo.OdooModels.Models;

public class HpCategory : HpBaseModel<HpCategory>
{
    internal readonly string[] UsualExcludedFields = [];
    public string Name;
    public string CatDescription;
    public bool TrackVersion;
    public bool TrackDepends;

    public HpCategory() { }
    public HpCategory(
        string name,
        string catDescription = "CAD files are versioned and have dependencies",
        bool trackVersion = true,
        bool trackDepends = true)
    {
        this.Name = name;
        this.CatDescription = catDescription;
        this.TrackVersion = trackVersion;
        this.TrackDepends = trackDepends;
    }
    public override string ToString()
    {
        return Name;
    }
}