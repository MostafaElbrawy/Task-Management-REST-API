using System.Runtime.Serialization;

namespace Task_Management.Enums
{
    public enum Priority
    {
        None,
        [EnumMember(Value = "Low")]
        Low,
        [EnumMember(Value = "Medium")]
        Medium,
        [EnumMember(Value = "High")]
        High
    }
}
