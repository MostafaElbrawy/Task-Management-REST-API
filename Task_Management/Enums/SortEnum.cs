using System.Runtime.Serialization;

namespace Task_Management.Enums
{
    public enum TaskSortColumn 
    {
        None,
        [EnumMember(Value = "DueDate")]
        DueDate,

        [EnumMember(Value = "Priority")]
        Priority,

        [EnumMember(Value = "CreatedAt")]
        CreatedAt
    }

    public enum SortOption 
    {
        None,
        [EnumMember(Value = "Asc")]
        Asc,

        [EnumMember(Value = "Desc")]
        Desc
    }
}
