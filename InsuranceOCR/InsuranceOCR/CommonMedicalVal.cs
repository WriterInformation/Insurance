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
    public string ProposalNo { get; set; }
    public string AppFormId { get; set; }
    public string MedicalPoint { get; set; }
    public string Result { get; set; }
}

public class Policy_Master
{
    public int PolicyNo { get; set; }
    public int PolicyStatusCode { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
}
public class Document_Master
{
    public int DocMasterID { get; set; }
    public int PolicyNo { get; set; }
    public int DocStatusCode { get; set; }
    public string Document_Path { get; set; }
    public string DocumentName { get; set; }
    public string DocType { get; set; }
    public string DocExtension { get; set; }
    public int TransID { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string ModifiedBy { get; set; }
}
public class KYC_Doc_Master
{
    public int DocID { get; set; }
    public int PolicyNo { get; set; }
    public string DocType { get; set; }
    public string Name { get; set; }
    public string IDNumber { get; set; }
    public string Address { get; set; }
    public DateTime DOB { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string ModifiedBy { get; set; }
}
public class Med_Report_Details
{
    public int ReportDetailsID { get; set; }
    public int ReportID { get; set; }
    public int TestName { get; set; }
    public double TestValue { get; set; }
    public double RangeFrom { get; set; }
    public double RangeTill { get; set; }
    public string HealthStatus { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string ModifiedBy { get; set; }
}
public class Med_Report_Master
{
    public int ReportID { get; set; }
    public int PolicyNo { get; set; }
    public string ReportType { get; set; }
    public string ReportName { get; set; }
    public DateTime ReportDate { get; set; }
    public int Age { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string ModifiedBy { get; set; }
}
public class Policy_Docs_Model
{
    public string[]? Files { get; set; }
    public int PolicyNo  { get; set; }
}

