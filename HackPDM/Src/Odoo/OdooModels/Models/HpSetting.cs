using System;


//using static System.Net.Mime.MediaTypeNames;



namespace HackPDM.Odoo.OdooModels.Models;

public class HpSetting : HpBaseModel<HpSetting>
{
	public string Name;
	public string Description;
	public string Type;
	public bool BoolValue;
	public int IntValue;
	public string CharValue;
	public float FloatValue;
	public DateTime DateValue;

	public HpSetting()
	{
	}
	public HpSetting(
		string name,
		string description,
		string type,
		bool boolValue=default,
		int intValue=default,
		string charValue=null,
		float floatValue=default,
		DateTime dateValue=default)
	{
		this.Name = name;
		this.Description = description;
		this.Type = type;
		this.BoolValue = boolValue;
		this.IntValue = intValue;
		this.CharValue = charValue;
		this.FloatValue = floatValue;
		this.DateValue = dateValue;
	}
}