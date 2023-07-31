using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceBPaaSWebAPI.Models
{
    public class Yearly_Finance_Master
    {
        [Key]
        public int YearlyFinance_Id { get; set; }

        [Display(Name = "Policy No")]
        public virtual int PolicyNo { get; set; }

        [ForeignKey("PolicyNo")]
        public virtual Policy_Master? policyMaster { get; set; }
        [Display(Name = "Document ID")]
        public virtual int DocMasterID { get; set; }

        [ForeignKey("DocMasterID")]
        public virtual Document_Master? document_Master { get; set; }
        public int? coi { get; set; }
        public int? Gross_AY { get; set; }
        public int? Form16_AY { get; set; }
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public Nullable<DateTime> ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }

}
