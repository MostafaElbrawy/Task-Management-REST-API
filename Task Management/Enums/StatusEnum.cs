using System.Runtime.Serialization;

namespace Task_Management.Enums
{
        public enum Status
        {
            None,

            [EnumMember(Value = "Todo")]
            Todo,

            [EnumMember(Value = "InProgress")]

            InProgress,

            [EnumMember(Value = "Done")]
            Done
        }

    
}
