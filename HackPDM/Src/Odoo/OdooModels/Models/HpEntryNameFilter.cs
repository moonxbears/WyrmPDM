//using static System.Net.Mime.MediaTypeNames;

namespace HackPDM.Odoo.OdooModels.Models;

public class HpEntryNameFilter : HpBaseModel<HpEntryNameFilter>
{
    public string NameProto;
    public string NameRegex;
    public string Description;

    public HpEntryNameFilter() { }
    public HpEntryNameFilter(
        string nameProto = null,
        string nameRegex = null,
        string description = null)
    {
        this.NameProto = nameProto;
        this.NameRegex = nameRegex;
        this.Description = description;
    }
}