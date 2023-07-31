using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceDAL
{
    public class Proposal_Master
    {
        [Key]
        public int ProposalNo { get; set; }
        [Display(Name = "Policy No")]
        public virtual int PolicyNo { get; set; }

        [ForeignKey("PolicyNo")]
        public virtual Policy_Master? policyMaster { get; set; }
        [Display(Name = "Document ID")]
        public virtual int DocMasterID { get; set; }

        [ForeignKey("DocMasterID")]
        public virtual Document_Master? document_Master { get; set; }
        public string? Title { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? DOB { get; set; }
        public string? Education { get; set; }
        public string? Occupation { get; set; }
        public string? Employer { get; set; }
        public Nullable<decimal> AnnualIncome { get; set; }
        public Nullable<decimal> PercentageShare { get; set; }
        public string? CurrentCity { get; set; }
        public Nullable<decimal> CurrentPinCode { get; set; }
        public Nullable<decimal> MobileNo { get; set; }
        public string? EmailID { get; set; }
        public string? PermanentCity { get; set; }
        public string? PermanentPinCode { get; set; }
        public Nullable<decimal> SumAssured { get; set; }
        public string? Gender { get; set; }
        public string? RelationWithProposer { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }

}
