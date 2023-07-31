using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceDAL
{
    public class Document_Master
    {
        [Key]
        public int DocMasterID { get; set; }

        [Display(Name = "Policy No")]
        public virtual int PolicyNo { get; set; }

        [ForeignKey("PolicyNo")]
        public virtual Policy_Master? policyMaster { get; set; }

        public int DocStatusCode { get; set; }
        public string? Document_Path { get; set; }
        public string? DocumentName { get; set; }
        public string? DocType { get; set; }
        public string? DocExtension { get; set; }
        public int TransID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public Nullable<DateTime> ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }
    public class Policy_Docs_Model
    {
        public string[]? Files { get; set; }
        public int PolicyNo { get; set; }
    }
}
