using System;
using System.Collections.Generic;
using System.Text;

using HackPDM.ClientUtils;

namespace HackPDM.Data
{
    public class EntryRow : IRowData
    {
        public int          ID          { get; set; }
        public string?      Name        { get; set; }
        public string?      Type        { get; set; }
        public long         Size        { get; set; }
        public FileStatus   Status      { get; set; }
        public HpUser?      Checkout    { get; set; }
        public HpCategory?  Category    { get; set; }
        public DateTime?    LocalDate   { get; set; }
        public DateTime?    RemoteDate  { get; set; }
        public string?      FullName    { get; set; }
    }
    public class HistoryRow : IRowData
    {
        public int          Version     { get; set; }
        public HpUser?      ModUser     { get; set; }
        public DateTime?    ModDate     { get; set; }
        public long         Size        { get; set; }
        public DateTime?    RelDate     { get; set; }
    }
    public class ParentRow : IRowData
    {
        public int          Version     { get; set; }
        public string?      Name        { get; set; }
        public string?      BasePath    { get; set; }
    }
    public class ChildrenRow : IRowData
    {
        public int          Version     { get; set; }
        public string?      Name        { get; set; }
        public string?      BasePath    { get; set; }
    }
    public class PropertesRow : IRowData
    {
        public int          Version     { get; set; }
        public string?      Configuration {  get; set; }
        public string?      Name        { get; set; }
        public string?      Property    { get; set; }
        public string?      Type        { get; set; }
    }
    public class VersionRow : IRowData
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        public long FileSize { get; set; }
        public int DirectoryID { get; set; }
        public int NodeID { get; set; }
        public int EntryID { get; set; }
        public int AttachmentID { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string? Checksum { get; set; }
        public string? OdooCompletePath { get; set; }
    }
    public class SearchRow : IRowData
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        public string? Directory { get; set; }
    }
    public class SearchPropRow : IRowData
    {
        public string? Name { get; set; }
        public string? Comparer { get; set; }
        public string? Value { get; set; }
    }
    public class FileTypeRow : IRowData
    {
        public string? Extension { get; set; }
        public string? Category { get; set; }
        public string? RegEx { get; set; }
        public string? Description { get; set; }
    }
    public class FileTypeEntryFilterRow : IRowData
    {
        public int ID { get; set; }
        public string? Proto { get; set; }
        public string? RegEx { get; set; }
        public string? Description { get; set; }
    }
    public class FileTypeLocRow : IRowData
    {
        public string? Extension { get; set; }
        public string? Status { get; set; }
        public string? Example { get; set; }
    }
    public class FileTypeLocDatRow : IRowData
    {
        public string? Extension { get; set; }
        public string? RegEx { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public object? Icon { get; set; } // Type is Unknown, so use object
        public object? RemoveIcon { get; set; } // Type is Unknown, so use object
    }
}
