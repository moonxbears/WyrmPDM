using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HackPDM.Extensions.General;
using HackPDM.Hack;


//using static System.Net.Mime.MediaTypeNames;



namespace HackPDM.Odoo.OdooModels.Models;

public class HpVersionProperty : HpBaseModel<HpVersionProperty>
{
    public string SwConfigName;
    public string TextValue;
    public float NumberValue;
    public bool YesnoValue;
    public string DateValue;
    public int VersionId;
    public int PropId;
    public string PropName;

    public HpVersionProperty() { }
    public HpVersionProperty(
        string swConfigName = null,
        string textValue = null,
        float numberValue = default,
        bool yesnoValue = default,
        string dateValue = null,
        int versionId = 0,
        int propId = 0)
    {
        this.SwConfigName = swConfigName;
        this.TextValue = textValue;
        this.NumberValue = numberValue;
        this.YesnoValue = yesnoValue;
        this.DateValue = dateValue;
        this.VersionId = versionId;
        this.PropId = propId;
    }
    public PropertyType GetValueType()
    {
        if (TextValue != null && TextValue != "" && TextValue != "False") return PropertyType.Text;
        if (DateValue != null && DateValue != "" && DateValue != "False") return PropertyType.Date;
        if (NumberValue != default) return PropertyType.Number;
        if (YesnoValue != default) return PropertyType.Yesno;
        return PropertyType.None;
    }
        
    public bool IsText( out string text )
    {
        PropertyType pType = GetValueType();
        text = null;
        if (pType == PropertyType.Text)
        {
            text = TextValue;
            return true;
        }
        return false;
    }
    public bool IsNumber( out float number )
    {
        PropertyType pType = GetValueType();
        number = default;
        if (pType == PropertyType.Number)
        {
            number = NumberValue;
            return true;
        }
        return false;
    }
    public bool IsYesNo(out bool yesNo)
    {
        PropertyType pType = GetValueType();
        yesNo = default;
        if (pType == PropertyType.Yesno)
        {
            yesNo = YesnoValue;
            return true;
        }
        return false;
    }
    public bool IsDate(out string date)
    {
        PropertyType pType = GetValueType();
        date = null;
        if (pType == PropertyType.Date)
        {
            date = TextValue;
            return true;
        }
        return false;
    }
    public bool IsNone()
    {
        bool isValue = true;
            
        isValue = IsText(out _);
        if (isValue) return false;
            
        isValue = IsNumber(out _);
        if (isValue) return false;

        isValue = IsDate(out _);
        if (isValue) return false;
            
        isValue = IsYesNo(out _);
        if (isValue) return false;

        return true;
    }
    public static async void Create(params HpVersion[] versions)
    {
        HpVersionProperty[] versionProperties = [];
        foreach (HpVersion version in versions)
        {
            try
            {
                if (!OdooDefaults.DependentExt.Contains($".{version.FileExt.ToUpper()}")) continue;
                string pathway = version.WinPathway;
                List<string> paths = [];
                List<Tuple<string, string, string, object>> props = HackDefaults.DocMgr.GetProperties(pathway);
                HpVersionProperty[] properties = [.. props.Select(prop =>
                {
                    bool isSuccessful = false;
                    if (!OdooDefaults.RestrictProperties | OdooDefaults.ExtToProp.TryGetValue(prop.Item2, out HpProperty hpProperty))
                    {
                        HpVersionProperty vProp = new()
                        {
                            SwConfigName = prop.Item1 == "" ? null : prop.Item1,
                            VersionId = version.Id != 0 ? version.Id : throw new Exception("version id not defined"),
                        };
                        if (hpProperty is not null) vProp.PropId = hpProperty.Id; 
                        switch (prop.Item3)
                        {
                            case "text": vProp.TextValue        = (string)prop.Item4; break;
                            case "date": vProp.DateValue        = (string)prop.Item4; break;
                            case "yesno": vProp.YesnoValue      = (bool)prop.Item4; break;
                            case "number": vProp.NumberValue    = (float)prop.Item4; break;
                        }
                        isSuccessful = true;
                        Debug.WriteLine($"prop: {prop.Item2} | {isSuccessful}");
                        return vProp;
                    }
                    Debug.WriteLine($"prop: {prop.Item2} | {isSuccessful}");
                    return null;
                })];
                versionProperties = [.. versionProperties, .. properties];
            }
            catch (Exception e)
            {
                Debug.WriteLine($"unable to create properties for {version.Id}\n{e}");
                return;
            }
        }
        if (versionProperties.Length > 0)
        {
            await MultiCreateAsync<HpVersionProperty>(versionProperties.ToArrayList());
        }
    }
    public enum PropertyType
    {
        Text,
        Number,
        Yesno,
        Date,
        None,
    }
}