using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceBPaaSWebAPI.Models
{
    public class KYC_Verify_Master
    {
        [Key]
        public int DocID { get; set; }
        [Display(Name = "Policy No")]
        public virtual int PolicyNo { get; set; }

        [ForeignKey("PolicyNo")]
        public virtual Policy_Master? policyMaster { get; set; }
        [Display(Name = "Document ID")]
        public virtual int DocMasterID { get; set; }

        [ForeignKey("DocMasterID")]
        public virtual Document_Master? document_Master { get; set; }
        public int? Name { get; set; }
        public int? IDNumber { get; set; }
        public int? City { get; set; }
        public int? Pincode { get; set; }
        public int? DOB { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

    }
}
