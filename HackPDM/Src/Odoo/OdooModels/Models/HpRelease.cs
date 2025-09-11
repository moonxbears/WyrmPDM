//using static System.Net.Mime.MediaTypeNames;

namespace HackPDM.Odoo.OdooModels.Models;

public class HpRelease : HpBaseModel<HpRelease>
{
    public string ReleaseNote;
    public int ReleaseUserId;

    public HpRelease() { }
    public HpRelease(
        string releaseNote,
        int releaseUserId = 0)
    {
        this.ReleaseNote = releaseNote;
        this.ReleaseUserId = releaseUserId;
    }
}