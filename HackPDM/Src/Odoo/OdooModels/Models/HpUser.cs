using System;


//using static System.Net.Mime.MediaTypeNames;



namespace HackPDM.Odoo.OdooModels.Models;

public class HpUser : HpBaseModel<HpUser>
{
	public string Name;
	public string Login;
	public string Email;
	public string Signature;
	public string NotificationType;
	public DateTime? LoginDate;
        
	public int? CompanyId;

	public bool? Active;

	public HpUser() {}

	public HpUser( string name,
		string login = null,
		string email = null,
		string signature = null,
		string notificationType = null,
		DateTime? loginDate = null,
		int? companyId = null,
		bool? active = null)
	{
		this.Name= name;
		this.Login= login;
		this.Email= email;
		this.Signature= signature;
		this.NotificationType= notificationType;
		this.LoginDate= loginDate;
		this.CompanyId= companyId;
		this.Active= active;
	}
	public override string ToString()
	{
		return Name;
	}
}