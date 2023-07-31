using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

internal class CommonMedicalVal
{
    public int Id { get; set; }
    public string? TestName { get; set; }
    public double HighestRangeFrom { get; set; }
    public double HighestRangeTill { get; set; }
    public string? Alternatives { get; set; }
    public bool valueCaptured { get; set; }
    public string? StringResult { get; set; }
    public bool IsSubMenu { get; set; }
    public bool IsMultiWord { get; set; }
    public string? MultiWordAlternatives { get; set; }
}
internal class OCRValues
{
    public string? TestName { get; set; }
    public string? TestValue { get; set; }
}
public class MedicalValues
{
    [Key]
    public string? ProposalNo { get; set; }
    public string? AppFormId { get; set; }
    public string? MedicalPoint { get; set; }
    public string? Result { get; set; }
}

public class Policy_Master
{
    public int PolicyNo { get; set; }
    public int PolicyStatusCode { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public Nullable<DateTime> ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
}
public class Document_Master
{
    public int docMasterID { get; set; }
    public int policyNo { get; set; }
    public int docStatusCode { get; set; }
    public string? document_Path { get; set; }
    public string? documentName { get; set; }
    public string? docType { get; set; }
    public string? docExtension { get; set; }
    public int transID { get; set; }
    public DateTime createdDate { get; set; }
    public string? createdBy { get; set; }
    public Nullable<DateTime> ModifiedDate { get; set; }
    public string? modifiedBy { get; set; }
}
public class KYC_Verify_Master
{
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
    public string CreatedBy { get; set; }
    public Nullable<System.DateTime> ModifiedDate { get; set; }
    public string ModifiedBy { get; set; }
}
public class Med_Report_Details
{
    public int ReportDetailsID { get; set; }
    public int ReportID { get; set; }
    public string? TestName { get; set; }
    public double NumericTestValue { get; set; }
    public double RangeFrom { get; set; }
    public double RangeTill { get; set; }
    public string? HealthStatus { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public Nullable<DateTime> ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public string? StringTestValue { get; set; }
}
public class Med_Report_Master
{
    public int ReportID { get; set; }
    public int PolicyNo { get; set; }
    public int DocMasterID { get; set; }
    public string? ReportType { get; set; }
    public string? ReportName { get; set; }
    public DateTime ReportDate { get; set; }
    public int Age { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
}
public class Policy_Docs_Model
{
    public string[]? Files { get; set; }
    public int PolicyNo  { get; set; }
}
public class Monthly_Finance_Master
{
    [Key]
    public int MonthlyFinance_Id { get; set; }

    [Display(Name = "Policy No")]
    public virtual int PolicyNo { get; set; }

    [ForeignKey("PolicyNo")]
    public virtual Policy_Master? policyMaster { get; set; }
    [Display(Name = "Document ID")]
    public virtual int DocMasterID { get; set; }

    [ForeignKey("DocMasterID")]
    public virtual Document_Master? document_Master { get; set; }

    public int? SalaryAmount { get; set; }
    public int? CrifIncome { get; set; }
    public int? CIBILScore { get; set; }
    //public int? CTCAnnualIncome { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public Nullable<DateTime> ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
}
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
public class Proposal_Master
{
    [Key]
    public int ProposalNo { get; set; }
    public int PolicyNo { get; set; }
    public int DocMasterID { get; set; }    
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

    public virtual Policy_Master Policy_Master { get; set; }
}

internal class ProposalValues
{
    public int Id { get; set; }
    public string FieldName { get; set; }
    public string Alternatives { get; set; }
    public bool valueCaptured { get; set; }
    public string StringResult { get; set; }
}
internal class KYCValues
{
    public int Id { get; set; }
    public string FieldName { get; set; }
    public string FieldValue { get; set; }
    public bool valueCaptured { get; set; }
    public string StringResult { get; set; }
}