using System.ComponentModel.DataAnnotations;

namespace InsuranceDAL
{
    public class Type_Master
    {
        [Key]
        public int TypeId { get; set; }
        public string? TypeName { get; set; }
    }
}
