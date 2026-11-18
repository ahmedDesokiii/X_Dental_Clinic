// ============================================================
// 📁 File: Models/DataTables/DataTableModels.cs
// ============================================================

namespace Dental.Models.DataTables
{
    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; } = 10;
        public List<DataTableColumn> Columns { get; set; } = new();
        public List<DataTableOrder> Order { get; set; } = new();
        public DataTableSearch Search { get; set; } = new();

        public string? CategoryFilter { get; set; }

        /// <summary>👥 H2: فلتر سريع لقائمة المرضى: balance | upcoming | inactive</summary>
        public string? QuickFilter { get; set; }
    }

    public class DataTableColumn
    {
        public string Data { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Searchable { get; set; } = true;
        public bool Orderable { get; set; } = true;
        public DataTableSearch Search { get; set; } = new();
    }

    public class DataTableOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; } = "asc";
    }

    public class DataTableSearch
    {
        public string Value { get; set; } = "";
        public bool Regex { get; set; }
    }

    public class DataTableResponse<T>
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public List<T> Data { get; set; } = new();
    }
}