using System.ComponentModel.DataAnnotations;

namespace InsuranceBPaaS_WebAPI.Models
{
    public class Type_Master
    {
        [Key]
        public int TypeId { get; set; }
        public string TypeName { get; set; }
    }
}
