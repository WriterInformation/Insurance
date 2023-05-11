internal class CommonMedicalVal
{
    public int Id { get; set; }
    public string TestName { get; set; }
    public double HighestRangeFrom { get; set; }
    public double HighestRangeTill { get; set; }
    public string Alternatives { get; set; }
    public bool valueCaptured { get; set; }
    public string StringResult { get; set; }
    public bool HasSubMenu { get; set; }
}
internal class OCRValues
{
    public string TestName { get; set; }
    public string TestValue { get; set; }
}

