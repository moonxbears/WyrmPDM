//using static System.Net.Mime.MediaTypeNames;

namespace HackPDM.Odoo.OdooModels.Models;

public class HpReleaseVersionRel : HpBaseModel<HpReleaseVersionRel>
{
    public int ReleaseId;
    public int ReleaseVersion;
    public int ReleaseUser;

    public HpReleaseVersionRel() { }
    public HpReleaseVersionRel(
        int releaseId = 0,
        int releaseVersion = 0,
        int releaseUser = 0)
    {
        this.ReleaseId = releaseId;
        this.ReleaseVersion = releaseVersion;
        this.ReleaseUser = releaseUser;
    }
}