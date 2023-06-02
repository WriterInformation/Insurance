using Microsoft.EntityFrameworkCore;

namespace InsuranceBPaaS_WebAPI.Models
{
    public class InsuranceDbContext: DbContext
    {
        public InsuranceDbContext(DbContextOptions<InsuranceDbContext> options) : base(options)
        {
        }
        public DbSet<Policy_Master> Policy_Master { get; set; }
        public DbSet<Document_Master> Document_Master { get; set; }
        public DbSet<KYC_Doc_Master> KYC_Doc_Master { get; set; }
        public DbSet<Med_Report_Master> Med_Report_Master { get; set; }
        public DbSet<Med_Report_Details> Med_Report_Details { get; set; }
        public DbSet<usp_AddEditMedicalValues> usp_AddEditMedicalValues { get; set; }
    }
}
