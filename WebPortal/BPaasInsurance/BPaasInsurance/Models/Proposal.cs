using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BPaasInsurance.Models
{
    public class Proposal
    {
        public int ProposalNo { get; set; }
        public string ProposalDate { get; set; }
        public string ProposalStatus { get; set; }
        public string ProposalDetail { get; set; }
    }
}