//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BPaasInsurance
{
    using System;
    using System.Collections.Generic;
    
    public partial class IncomeAllowableFactor
    {
        public int IAF_Id { get; set; }
        public Nullable<int> AgeFrom { get; set; }
        public Nullable<int> AgeTill { get; set; }
        public Nullable<int> MultiplierFactor { get; set; }
        public Nullable<int> Income_Above_5L { get; set; }
    }
}