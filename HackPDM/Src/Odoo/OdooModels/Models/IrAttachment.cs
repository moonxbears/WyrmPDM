using System.Collections;
//using static System.Net.Mime.MediaTypeNames;


using OClient = HackPDM.Odoo.OdooClient;

namespace HackPDM.Odoo.OdooModels.Models;

public class IrAttachment : HpBaseModel<IrAttachment>
{
    public string Name;
    public int ResId;
    public int FileSize;
    public string ResModel;
    public string Checksum;
    public string Mimetype;
    public string Type;

    private string _fileContentsBase64;
    public IrAttachment() { }
    public IrAttachment(
        string name,
        int resId = 0,
        int fileSize = 0,
        string resModel = null,
        string checksum = null,
        string mimetype = null,
        string type = "binary",
        string fileContentsBase64 = null)
    {
        this.Name = name;
        this.ResId = resId;
        this.FileSize = fileSize;
        this.ResModel = resModel;
        this.Checksum = checksum;
        this.Mimetype = mimetype;
        this.Type = type;
        this._fileContentsBase64 = fileContentsBase64;
    }

    public string DownloadContents()
    {
        const string datas = "datas";
        if (this.IsRecord || this.Id != 0)
        {
            // reads the datas field in ir.attachment and returns an ArrayList with one record because of one ID
            // which contains a hashtable with keys: datas and id. datas has a value of string which is the base 64 file contents
            this._fileContentsBase64 = (string)((Hashtable)OClient.Read(HpModel, [this.Id], [datas])[0])[datas];
            return this._fileContentsBase64;
        }
        return null;
    }
        
    public string GetFileContentsB64() => _fileContentsBase64;
}