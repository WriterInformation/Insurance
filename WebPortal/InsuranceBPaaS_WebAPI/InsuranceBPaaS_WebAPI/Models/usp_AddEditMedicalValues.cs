using System.ComponentModel.DataAnnotations;

namespace InsuranceBPaaS_WebAPI.Models
{
    public class usp_AddEditMedicalValues
    {
        [Key]
        public string ProposalNo { get; set; }
        public string AppFormId { get; set; }
        public string MedicalPoint { get; set; }
        public string Result { get; set; }
    }
}
