//using static System.Net.Mime.MediaTypeNames;

namespace HackPDM.Odoo.OdooModels.Models;

public class HpProperty : HpBaseModel<HpProperty>
{
    public string Name;
    public string PropType;
    public bool Active;

    public HpProperty() { }
    public HpProperty(
        string name,
        string propType = null,
        bool active = default)
    {
        this.Name = name;
        this.PropType = propType;
        this.Active = active;
    }
    public override string ToString()
    {
        return Name;
    }
}