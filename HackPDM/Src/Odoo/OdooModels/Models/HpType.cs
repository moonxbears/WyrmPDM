using System.Drawing;
using HackPDM.Extensions.General;


//using static System.Net.Mime.MediaTypeNames;



namespace HackPDM.Odoo.OdooModels.Models;

public class HpType : HpBaseModel<HpType>
{
	public string Description;
	public string FileExt;
	public string Icon;
	public string TypeRegex;
	public int CatId;
	public Image ImageSave {get;set;}

	public HpType()
	{ 
	}
	public HpType(
		string description = null,
		string fileExt = null,
		string iconBase64 = null,
		string typeRegex = null,
		int catId = 0)
	{
		this.Description = description;
		this.FileExt = fileExt; 
		this.Icon = iconBase64;
		this.TypeRegex = typeRegex;
		this.CatId = catId;
		this.ImageSave = null;
	}
	public HpType(
		string description = null,
		string fileExt = null,
		Image icon = null,
		string typeRegex = null,
		int catId = 0,
		bool saveImageType = false)
	{
		this.Description = description;
		this.FileExt = fileExt;
		this.TypeRegex = typeRegex;
		this.CatId = catId;

		if (saveImageType) this.ImageSave = icon;
		else this.ImageSave = null;

		this.Icon = icon?.ToBase64String();
	}
}