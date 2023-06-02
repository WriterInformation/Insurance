using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceBPaaS_WebAPI.Models
{
    public class Policy_Master
    {
        [Key]
        public int PolicyNo { get; set; }

        [Display(Name = "Status")]
        public virtual int PolicyStatusCode { get; set; }

        [ForeignKey("PolicyStatusCode")]
        public virtual Type_Master? Type_Master { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
