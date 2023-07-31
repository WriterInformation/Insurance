using System.ComponentModel.DataAnnotations;

namespace InsuranceBPaaSWebAPI.Models
{
    public class DocType_Master
    {
        [Key]
        public int DocTypeId { get; set; }
        public string? DocType { get; set; }
        public string? FileNames { get; set; }
    }
}
