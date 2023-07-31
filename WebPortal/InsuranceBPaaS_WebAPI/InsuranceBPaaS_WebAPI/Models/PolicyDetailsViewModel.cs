namespace InsuranceBPaaSWebAPI.Models
{
    public class PolicyDetailsViewModel
    {
        public List<Med_Report_Details> med_Report_Details { get; set; }
        public List<Document_Master> document_Master { get; set; }
        public List<Monthly_Finance_Master> monthly_Finance_Master { get; set; }
        public List<Yearly_Finance_Master> yearly_Finance_Master { get; set; }
        public List<Med_Report_Master> med_Report_Master { get; set; }
        public Proposal_Master proposal_Master { get; set; }
        public KYC_Verify_Master kyc_Verify_Master { get; set; }
    }
}
