using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceBPaaS_WebAPI.Models
{
    public class Finance_Master
    {
        [Key]
        public int FinanceMasterId { get; set; }

        [Display(Name = "Policy No")]
        public virtual int PolicyNo { get; set; }

        [ForeignKey("PolicyNo")]
        public virtual Policy_Master? policyMaster { get; set; }
        [Display(Name = "Document ID")]
        public virtual int DocMasterID { get; set; }

        [ForeignKey("DocMasterID")]
        public virtual Document_Master? document_Master { get; set; }

        public int? SalaryAmount { get; set; }
        public int? coi { get; set; }
        public int? Gross_AY { get; set; }
        public int? Form16_AY { get; set; }
        public int? CrifIncome { get; set; }
        public int? CIBILScore { get; set; }
        public int? CTCAnnualIncome { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public Nullable<DateTime> ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }

}
