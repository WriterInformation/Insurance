using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceBPaaS_WebAPI.Models
{
    public class Med_Report_Master
    {
        [Key]
        public int ReportID { get; set; }
        [Display(Name = "Policy No")]
        public virtual int PolicyNo { get; set; }

        [ForeignKey("PolicyNo")]
        public virtual Policy_Master policyMaster { get; set; }
        public string ReportType { get; set; }
        public string ReportName { get; set; }
        public DateTime ReportDate { get; set; }
        public int Age { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }
}
