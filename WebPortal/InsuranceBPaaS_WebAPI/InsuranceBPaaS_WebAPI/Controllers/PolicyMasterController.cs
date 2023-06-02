using InsuranceBPaaS_WebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

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

        // GET: api/VendorAPI
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Policy_Master>>> GetPolicy_Master()
        {
            return await _context.Policy_Master.ToListAsync();
        }
        // GET: api/VendorAPI
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Document_Master>>> GetPendingOCRData()
        {
            return await _context.Document_Master.Where(x=>x.DocStatusCode==1).ToListAsync();
        }
        // GET: api/VendorAPI
        [HttpGet]
        public async Task<ActionResult<IEnumerable<KYC_Doc_Master>>> GetKYC_Doc_Master()
        {
            return await _context.KYC_Doc_Master.ToListAsync();
        }
        // GET: api/VendorAPI
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Med_Report_Details>>> GetMed_Report_Details()
        {
            return await _context.Med_Report_Details.ToListAsync();
        }
        // GET: api/VendorAPI/5
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

        // PUT: api/VendorAPI/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVendor(int id, Policy_Master policy_Master)
        {
            if (id != policy_Master.PolicyNo)
            {
                return BadRequest();
            }

            _context.Entry(policy_Master).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VendorExists(id))
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
        public async Task<ActionResult<Policy_Master>> CheckPolicyMaster(string[] dirs)
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
                            Document_Master document_Master = new Document_Master();
                            document_Master.PolicyNo = policy_Doc.PolicyNo;
                            document_Master.DocStatusCode = 1;
                            document_Master.Document_Path = doc;
                            document_Master.DocumentName = docName;
                            document_Master.DocType = "Medical";
                            document_Master.DocExtension = docName.Split('.')[1];
                            document_Master.TransID = 1;
                            document_Master.CreatedDate = DateTime.Now;
                            document_Master.CreatedBy = "SYSTEM";
                            _context.Document_Master.Add(document_Master);
                            await _context.SaveChangesAsync();
                        }
                    }
                    return Ok();
                }
            }
            catch(Exception ex)
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

        [HttpPost]
        public async Task<IActionResult> AddNewPolicy(Document_Master documentMaster)
        {
            await PostPolicyMaster(documentMaster.policyMaster);
            _context.Document_Master.Add(documentMaster);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpPost]
        public async Task<IActionResult> AddMedicalValues(usp_AddEditMedicalValues medVal)
        {
            await _context.Database.ExecuteSqlAsync($"usp_AddEditMedicalValues @appFormId={medVal.AppFormId},@proposalNo ={medVal.ProposalNo},@medPointName={medVal.MedicalPoint}, @medPointResult={medVal.Result}");
            return NoContent();
        }

        [HttpPost]
        public async Task<IActionResult> AddMedicalValuesList(List<usp_AddEditMedicalValues> medValList)
        {
            foreach(var medVal in medValList)
            {
                _ = AddMedicalValues(medVal);
            }
            return NoContent();
        }

        // DELETE: api/VendorAPI/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVendor(int id)
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

        private bool VendorExists(int id)
        {
            return _context.Policy_Master.Any(e => e.PolicyNo == id);
        }
    }
}
