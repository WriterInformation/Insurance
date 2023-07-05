using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceBPaaS_WebAPI.Models
{
    public class Med_Report_Details
    {
        [Key]
        public int ReportDetailsID { get; set; }

        [Display(Name = "Report ID")]
        public virtual int ReportID { get; set; }

        [ForeignKey("ReportID")]
        public virtual Med_Report_Master? medReportMaster { get; set; }
        public string? TestName { get; set; }
        public Nullable<decimal> NumericTestValue { get; set; }
        public Nullable<decimal> RangeFrom { get; set; }
        public Nullable<decimal> RangeTill { get; set; }
        public string? HealthStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public Nullable<DateTime> ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public string? StringTestValue { get; set; }
    }
}
