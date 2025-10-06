//using static System.Net.Mime.MediaTypeNames;

using System;
// ReSharper disable InconsistentNaming

namespace HackPDM.Odoo.OdooModels.Models;

public class HpRelease : HpBaseModel<HpRelease>
{
    public string release_note;
    public DateTime? release_stamp;
    public int release_user_id;

    public HpRelease() { }
    public HpRelease(
        string releaseNote,
        int releaseUserId = 0,
        DateTime releaseStamp = default)
    {
        this.release_note = releaseNote;
        this.release_user_id = releaseUserId;
        this.release_stamp = releaseStamp;
    }
}