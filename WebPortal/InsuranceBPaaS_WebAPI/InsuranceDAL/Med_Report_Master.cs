using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceDAL
{
    public class Med_Report_Master
    {
        [Key]
        public int ReportID { get; set; }
        [Display(Name = "Policy No")]
        public virtual int PolicyNo { get; set; }

        [ForeignKey("PolicyNo")]
        public virtual Policy_Master? policyMaster { get; set; }
        [Display(Name = "Document ID")]
        public virtual int DocMasterID { get; set; }

        [ForeignKey("DocMasterID")]
        public virtual Document_Master? document_Master { get; set; }
        public string? ReportType { get; set; }
        public string? ReportName { get; set; }
        public DateTime ReportDate { get; set; }
        public int Age { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public Nullable<DateTime> ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
