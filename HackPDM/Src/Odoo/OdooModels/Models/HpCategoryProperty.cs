//using static System.Net.Mime.MediaTypeNames;

namespace HackPDM.Odoo.OdooModels.Models;

public class HpCategoryProperty : HpBaseModel<HpCategoryProperty>
{
    public int CatId;
    public int PropId;

    public HpCategoryProperty() { }
    public HpCategoryProperty(
        int catId = 0,
        int propId = 0)
    {
        this.CatId = catId;
        this.PropId = propId;
    }
}