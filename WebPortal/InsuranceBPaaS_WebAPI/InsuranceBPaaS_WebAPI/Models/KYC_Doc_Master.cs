using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceBPaaS_WebAPI.Models
{
    public class KYC_Doc_Master
    {
        [Key]
        public int DocID { get; set; }
        [Display(Name = "Policy No")]
        public virtual int PolicyNo { get; set; }

        [ForeignKey("PolicyNo")]
        public virtual Policy_Master? policyMaster { get; set; }
        public string? DocType { get; set; }
        public string? Name { get; set; }
        public string? IDNumber { get; set; }
        public string? Address { get; set; }
        public DateTime DOB { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
