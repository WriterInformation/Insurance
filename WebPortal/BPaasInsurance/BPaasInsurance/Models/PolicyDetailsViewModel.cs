using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BPaasInsurance.Models
{
    public class PolicyDetailsViewModel
    {
        public List<Med_Report_Details> med_Report_Details { get; set; }
        public List<Document_Master> document_Master { get; set; }
    }
}