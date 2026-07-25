//using Newtonsoft.Json;
//using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;
using System.Text.Json;
using Task_Management.Enums;
namespace Task_Management.DTOs
{
    public class UpdateTaskDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; } 
        public Priority? Priority { get; set; }
        public Status? Status { get; set; }
        public DateTime? DueDate {  get; set; }
        public int ProjectId { get; set; }
    }
}
