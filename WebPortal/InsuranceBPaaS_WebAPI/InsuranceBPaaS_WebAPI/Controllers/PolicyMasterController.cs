using InsuranceBPaaS_WebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Data.SqlTypes;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Threading.Channels;
using InsuranceDAL;
using System.Reflection.Metadata;
using Microsoft.Data.SqlClient;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace InsuranceBPaaS_WebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PolicyMasterAPIController : ControllerBase
    {
        private readonly InsuranceDbContext _context;

        public PolicyMasterAPIController(InsuranceDbContext context)
        {
            _context = context;
        }

        #region GetData
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Policy_Master>>> GetPolicy_Master()
        {
            return await _context.Policy_Master.ToListAsync();
        }

        [HttpGet]
        public async Task<IEnumerable<Med_Report_Details>> GetPolicyReportDetails(int reportId)
        {
            var lst_medReportDetails = await _context.Med_Report_Details.ToListAsync();
            return lst_medReportDetails.Where(x => x.ReportID == reportId);
        }
        [HttpGet]
        public async Task<PolicyDetailsViewModel> GetPolicyDetails(int policyNo)
        {
            var medReportDetails = _context.Med_Report_Details.Where(x => x.medReportMaster.PolicyNo == policyNo).ToList();
            var documentMaster = _context.Document_Master.Where(x => x.PolicyNo == policyNo).ToList();
            var monthlyFinanceMaster = _context.Monthly_Finance_Master.Where(x => x.PolicyNo == policyNo).ToList();
            var yearlyFinanceMaster = _context.Yearly_Finance_Master.Where(x => x.PolicyNo == policyNo).ToList();
            var medReportMaster = _context.Med_Report_Master.Where(x => x.PolicyNo == policyNo).ToList();
            var proposalMaster = _context.Proposal_Master.Where(x => x.PolicyNo == policyNo).First();
            var kyc_Verify_Master = _context.KYC_Verify_Master.Where(x => x.PolicyNo == policyNo).First();

            PolicyDetailsViewModel policyDetailsViewModel = new PolicyDetailsViewModel
            {
                med_Report_Details = medReportDetails,
                document_Master = documentMaster,
                monthly_Finance_Master = monthlyFinanceMaster,
                yearly_Finance_Master = yearlyFinanceMaster,
                med_Report_Master = medReportMaster,
                proposal_Master = proposalMaster,
                kyc_Verify_Master = kyc_Verify_Master
            };
            return policyDetailsViewModel;
        }

        [HttpPost]
        public async Task<IEnumerable<Med_Report_Details>> GetBloodReportDetails(Med_Report_Master med_Report_Master)
        {
            List<Med_Report_Details> med_Report_Details = new List<Med_Report_Details>();
            var reportIds = _context.Med_Report_Master.Where(x => x.PolicyNo == med_Report_Master.PolicyNo && x.ReportType == "Medical").Select(x => x.ReportID).ToList();
            return await GetPolicyReportDetails(reportIds[1]);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Document_Master>>> GetPendingOCRData()
        {
            return await _context.Document_Master.Where(x => x.DocStatusCode == 0 && x.DocType != "Others").ToListAsync();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<KYC_Verify_Master>>> GetKYC_Verify_Master()
        {
            return await _context.KYC_Verify_Master.ToListAsync();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Med_Report_Details>>> GetMed_Report_Details()
        {
            return await _context.Med_Report_Details.ToListAsync();
        }
        #endregion

        #region Manage Policies & Docs
        [HttpPost]
        public ActionResult<Policy_Master> CheckPolicyMaster(string[] dirs)
        {
            var policies = _context.Policy_Master.ToList();
            foreach (string dir in dirs)
            {
                string directoryName = dir.Split(@"\")[dir.Split(@"\").Length - 1];
                bool exists = policies.Exists(x => Convert.ToString(x.PolicyNo) == directoryName);
                if (!exists)
                {
                    _context.Policy_Master.Add(new Policy_Master()
                    {
                        PolicyNo = Convert.ToInt32(directoryName),
                        PolicyStatusCode = 0,
                        CreatedBy = "SYSTEM",
                        CreatedDate = DateTime.Now,
                    });
                    _context.SaveChanges();
                }
            }
            return Ok();
        }
        [HttpPost]
        public async Task<ActionResult<Policy_Master>> ManageDocsForPolicies(List<Policy_Docs_Model> policy_Docs_Models)
        {
            try
            {
                foreach (var policy_Doc in policy_Docs_Models)
                {
                    var docsStoredInDb = _context.Document_Master.ToList().FindAll(x => x.PolicyNo == policy_Doc.PolicyNo);
                    foreach (string doc in policy_Doc.Files)
                    {
                        if (!docsStoredInDb.Exists(x => x.Document_Path == doc))
                        {
                            string docName = doc.Split(@"\")[doc.Split(@"\").Length - 1];
                            string docTypeName = "Others";
                            var doctypes = _context.DocType_Master.ToList();
                            foreach (var doctype in doctypes)
                            {
                                string[] filenames = doctype.FileNames.ToUpper().Split('|');
                                bool match = Array.Exists(filenames, x => docName.Split('.')[0].ToUpper().Contains(x));
                                if (match)
                                {
                                    docTypeName = doctype.DocType;
                                }
                            }
                            Document_Master document_Master = new Document_Master();
                            document_Master.PolicyNo = policy_Doc.PolicyNo;
                            document_Master.DocStatusCode = 0;
                            document_Master.Document_Path = doc;
                            document_Master.DocumentName = docName;
                            document_Master.DocType = docTypeName;
                            document_Master.DocExtension = docName.Split('.')[1];
                            document_Master.TransID = 1;
                            document_Master.CreatedDate = DateTime.Now;
                            document_Master.CreatedBy = "SYSTEM";
                            _context.Document_Master.Add(document_Master);
                            await _context.SaveChangesAsync();
                        }
                    }
                    //return Ok();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok();
        }
        [HttpPost]
        public async Task<ActionResult<Policy_Master>> PostPolicyMaster(Policy_Master policy_Master)
        {
            _context.Policy_Master.Add(policy_Master);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetVendor", new { id = policy_Master.PolicyNo }, policy_Master);
        }
        #endregion

        #region Add
        [HttpPost]
        public async Task<IActionResult> AddNewPolicy(Document_Master documentMaster)
        {
            await PostPolicyMaster(documentMaster.policyMaster);
            _context.Document_Master.Add(documentMaster);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost]
        public int AddMedicalReport(Med_Report_Master med_Report)
        {
            med_Report.CreatedDate = DateTime.Now;
            med_Report.ModifiedDate = null;
            med_Report.CreatedBy = "SYSTEM";
            _context.Med_Report_Master.Add(med_Report);
            _context.SaveChanges();
            int id = med_Report.ReportID;
            return id;
        }
        [HttpPost]
        public IActionResult AddMedicalValues(List<Med_Report_Details> lst_Med_Report_Details)
        {
            foreach (var medReport in lst_Med_Report_Details)
            {
                _context.Med_Report_Details.Add(medReport);
                _context.SaveChanges();
                int docID = _context.Med_Report_Master.Where(x => x.ReportID == medReport.ReportID).First().DocMasterID;
                var entity = _context.Document_Master.FirstOrDefault(e => e.DocMasterID == docID);
                if (entity != null)
                {
                    // Modify the desired property
                    entity.DocStatusCode = 1;
                    // Save the changes to the database
                    _context.SaveChanges();
                }
            }
            return NoContent();
        }
        [HttpPost]
        public IActionResult AddMonthlyFinanceValues(Monthly_Finance_Master monthly_Finance_Master)
        {
            _context.Monthly_Finance_Master.Add(monthly_Finance_Master);
            _context.SaveChanges();
            var entity = _context.Document_Master.FirstOrDefault(e => e.DocMasterID == monthly_Finance_Master.DocMasterID);
            if (entity != null)
            {
                // Modify the desired property
                entity.DocStatusCode = 1;
                // Save the changes to the database
                _context.SaveChanges();
            }
            return NoContent();
        }
        [HttpPost]
        public IActionResult AddYearlyFinanceValues(Yearly_Finance_Master yearly_Finance_Master)
        {
            _context.Yearly_Finance_Master.Add(yearly_Finance_Master);
            _context.SaveChanges();
            var entity = _context.Document_Master.FirstOrDefault(e => e.DocMasterID == yearly_Finance_Master.DocMasterID);
            if (entity != null)
            {
                // Modify the desired property
                entity.DocStatusCode = 1;
                // Save the changes to the database
                _context.SaveChanges();
            }
            return NoContent();
        }
        [HttpPost]
        public IActionResult AddProposalValues(Proposal_Master proposal_Master)
        {
            _context.Proposal_Master.Add(proposal_Master);
            _context.SaveChanges();
            var entity = _context.Document_Master.FirstOrDefault(e => e.DocMasterID == proposal_Master.DocMasterID);
            if (entity != null)
            {
                // Modify the desired property
                entity.DocStatusCode = 1;
                // Save the changes to the database
                _context.SaveChanges();
            }
            return NoContent();
        }

        [HttpPost]
        public IActionResult AddkycOCRResult(KYC_Verify_Master kyc_Verify_Master)
        {
            _context.KYC_Verify_Master.Add(kyc_Verify_Master);
            _context.SaveChanges();
            var entity = _context.Document_Master.FirstOrDefault(e => e.DocMasterID == kyc_Verify_Master.DocMasterID);
            if (entity != null)
            {
                // Modify the desired property
                entity.DocStatusCode = 1;
                // Save the changes to the database
                _context.SaveChanges();
            }
            return NoContent();
        }
        [HttpPost]
        public async Task<IActionResult> AddECGReportValues(Med_Report_Details med_Report_Details)
        {
            _context.Med_Report_Details.Add(med_Report_Details);
            _context.SaveChanges();
            int docID = _context.Med_Report_Master.Where(x => x.ReportID == med_Report_Details.ReportID).First().DocMasterID;
            var entity = _context.Document_Master.FirstOrDefault(e => e.DocMasterID == docID);
            if (entity != null)
            {
                // Modify the desired property
                entity.DocStatusCode = 1;
                // Save the changes to the database
                _context.SaveChanges();
            }
            return NoContent();
        }
        #endregion

        #region Edit
        [HttpPost]
        public async Task<IActionResult> EditMedicalReportValues(List<Med_Report_Details> lst_med_Report_Details)
        {
            foreach (var med_Report_Details in lst_med_Report_Details)
            {
                med_Report_Details.ModifiedDate = DateTime.Now;
                med_Report_Details.ModifiedBy = "SYSTEM";
                if (_context.Med_Report_Details.Select(x => x.ReportDetailsID == med_Report_Details.ReportDetailsID).Count() <= 0)
                {
                    return BadRequest();
                }
                _context.Entry(med_Report_Details).State = EntityState.Modified;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Med_Report_Details.Any(e => e.ReportDetailsID == med_Report_Details.ReportDetailsID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return NoContent();
        }
        [HttpPost]
        public async Task<IActionResult> EditECGReportValues(Med_Report_Details med_Report_Details)
        {
            med_Report_Details.ModifiedDate = DateTime.Now;
            med_Report_Details.ModifiedBy = "SYSTEM";
            if (_context.Med_Report_Details.Select(x => x.ReportDetailsID == med_Report_Details.ReportDetailsID).Count() <= 0)
            {
                return BadRequest();
            }
            _context.Entry(med_Report_Details).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Med_Report_Details.Any(e => e.ReportDetailsID == med_Report_Details.ReportDetailsID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return NoContent();
        }
        [HttpPost]
        public async Task<IActionResult> EditMonthly_FinancialValues(List<Monthly_Finance_Master> lstMonthly_Finance_Master)
        {
            foreach (var monthly_Finance_Master in lstMonthly_Finance_Master)
            {
                monthly_Finance_Master.ModifiedDate = DateTime.Now;
                monthly_Finance_Master.ModifiedBy = "SYSTEM";
                if (_context.Monthly_Finance_Master.Where(x => x.MonthlyFinance_Id == monthly_Finance_Master.MonthlyFinance_Id).Count() <= 0)
                {
                    return BadRequest();
                }
                _context.Entry(monthly_Finance_Master).State = EntityState.Modified;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Monthly_Finance_Master.Any(e => e.MonthlyFinance_Id == monthly_Finance_Master.MonthlyFinance_Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            var param = new SqlParameter("@PolicyNo", lstMonthly_Finance_Master.First().PolicyNo);
            await _context.Database.ExecuteSqlRawAsync(@"exec InsertCaseSummary @PolicyNo", param);

            return NoContent();

        }
        [HttpPost]
        public async Task<IActionResult> EditProposalValues(Proposal_Master proposal_Master)
        {
            proposal_Master.ModifiedDate = DateTime.Now;
            proposal_Master.ModifiedBy = "SYSTEM";
            if (_context.Proposal_Master.Where(x => x.ProposalNo == proposal_Master.ProposalNo).Count() <= 0)
            {
                return BadRequest();
            }
            _context.Entry(proposal_Master).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Proposal_Master.Any(e => e.ProposalNo == proposal_Master.ProposalNo))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return NoContent();
        }
        #endregion

        [HttpGet]
        public async Task<ActionResult<Proposal_Master>> GetProposalDetailsByPolicyNo(int id)
        {
            var proposal_Master = _context.Proposal_Master.Where(x => x.PolicyNo == id).First();

            if (proposal_Master == null)
            {
                return NotFound();
            }

            return proposal_Master;
        }

        [HttpPost]
        public int GetDocMasterIdForECG(int policyNo)
        {
            int docMasterId = 0;
            var entity = _context.Document_Master.FirstOrDefault(e => e.PolicyNo == policyNo && e.DocumentName.Contains("ECG"));
            if (entity != null)
            {
                docMasterId = entity.DocMasterID;
            }
            return docMasterId;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Policy_Master>> GetVendor(int id)
        {
            var policy_Master = await _context.Policy_Master.FindAsync(id);

            if (policy_Master == null)
            {
                return NotFound();
            }

            return policy_Master;
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePolicy(int id)
        {
            var policy_Master = await _context.Policy_Master.FindAsync(id);
            if (policy_Master == null)
            {
                return NotFound();
            }

            _context.Policy_Master.Remove(policy_Master);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
