using System;
using BPaasInsurance.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Configuration;
//using InsuranceDAL;

namespace BPaasInsurance.Controllers
{
    public class HomeController : Controller
    {
        //public ActionResult Index()
        //{
        //    var proposal = getProposal();

        //    return View(proposal);
        //}

        static string webApiUrl = ConfigurationManager.AppSettings["webapi_url"].ToString();

        public ActionResult Index(int page = 1, string sort = "FirstName", string sortdir = "asc", string search = "")
        {
            int pageSize = 10;
            int totalRecord = 0;
            if (page < 1) page = 1;
            int skip = (page * pageSize) - pageSize;
            var data = GetEmployees(search, sort, sortdir, skip, pageSize, out totalRecord);
            ViewBag.TotalRows = totalRecord;
            ViewBag.search = search;
            ViewBag.Title = "Policies";
            return View(data);
        }

        public List<Policy_Master> GetEmployees(string search, string sort, string sortdir, int skip, int pageSize, out int totalRecord)
        {
            using (InsuranceBPaaSEntities dc = new InsuranceBPaaSEntities())
            {
                var v = (from a in dc.Policy_Master
                         where
                                 a.PolicyNo.ToString().Contains(search) ||
                                 a.CreatedDate.ToString().Contains(search) ||
                                 a.PolicyStatusCode.ToString().Contains(search)
                         select a
                                );
                totalRecord = v.Count();
                v = v.OrderBy(x => x.PolicyNo.ToString());
                if (pageSize > 0)
                {
                    v = v.Skip(skip).Take(pageSize);
                }
                return v.ToList();
            }
        }
        public async Task<PolicyDetailsViewModel> GetPolicyDetailsAsync(int? PolicyNo)
        {
            var policy_Details_Response = await SendHttpRequest("", "PolicyMasterAPI/GetPolicyDetails?policyNo=" + PolicyNo, Convert.ToInt32(Enums.HttpMethod.GET));
            PolicyDetailsViewModel policyDetailsViewModel = new PolicyDetailsViewModel();
            if (policy_Details_Response.IsSuccessStatusCode)
            {
                var policy_Details_Response_Content = await policy_Details_Response.Content.ReadAsStringAsync();
                policyDetailsViewModel = JsonSerializer.Deserialize<PolicyDetailsViewModel>(policy_Details_Response_Content);
            }
            else
            {
                // Handle the error response
                Console.WriteLine("Error: " + policy_Details_Response.StatusCode);
            }
            return policyDetailsViewModel;
        }
        public PolicyDetailsViewModel GetPolicyDetails(int? PolicyNo)
        {
            GetPolicyDetailsAsync(PolicyNo);
            PolicyDetailsViewModel policyDetailsViewModel;
            using (InsuranceBPaaSEntities dc = new InsuranceBPaaSEntities())
            {
                var v = (from a in dc.Med_Report_Details
                         where
                                 a.Med_Report_Master.PolicyNo.ToString().Contains(PolicyNo.ToString())
                         select a
                                );
                var medMaster = (from a in dc.Med_Report_Master
                         where
                                 a.PolicyNo.ToString().Contains(PolicyNo.ToString())
                         select a
                                );
                //totalRecord = v.Count();
                //v = v.OrderBy(x => x.PolicyNo.ToString());
                //if (pageSize > 0)
                //{
                //    v = v.Skip(skip).Take(pageSize);
                //}
                var doc = (from a in dc.Document_Master
                         where
                                 a.PolicyNo.ToString().Contains(PolicyNo.ToString())
                         select a
                                );
                //var finance = (from a in dc.Finance_Master
                //           where
                //                   a.PolicyNo.ToString().Contains(PolicyNo.ToString())
                //           select a
                //                );
                var monthFinance = (from a in dc.Monthly_Finance_Master
                                  where
                                          a.PolicyNo.ToString().Contains(PolicyNo.ToString())
                                  select a
                                );
                var yearFinance = (from a in dc.Yearly_Finance_Master
                                  where
                                          a.PolicyNo.ToString().Contains(PolicyNo.ToString())
                                  select a
                                );
                var proposalDetails = (from a in dc.Proposal_Master
                               where
                                       a.PolicyNo.ToString().Contains(PolicyNo.ToString())
                               select a
                                );
                var kycDetails = (from a in dc.KYC_Verify_Master
                                       where
                                               a.PolicyNo.ToString().Contains(PolicyNo.ToString())
                                       select a
                                );
                var caseSummary = (from a in dc.CaseSummaries
                                  where
                                          a.PolicyNo.ToString().Contains(PolicyNo.ToString())
                                  select a
                                );
                policyDetailsViewModel = new PolicyDetailsViewModel
                {
                    med_Report_Details = v.ToList(),
                    med_Report_Master = medMaster.ToList(),
                    document_Master = doc.ToList(),
                    monthly_Finance_Master = monthFinance.ToList(),
                    yearly_Finance_Master = yearFinance.ToList()
                    //finance_Master = finance.ToList()
                };
                foreach(var item in policyDetailsViewModel.med_Report_Details)
                {
                    item.Med_Report_Master = medMaster.Where(x => x.ReportID == item.ReportID).First();
                }
                if (proposalDetails.Count() != 0)
                {
                    policyDetailsViewModel.proposal_Master = proposalDetails.First();
                }
                if (kycDetails.Count() != 0)
                {
                    policyDetailsViewModel.kyc_Verify_Master = kycDetails.First();
                    ViewBag.kyc_Verify_Master = kycDetails.First();
                }
                if (caseSummary.Count() != 0)
                {
                    policyDetailsViewModel.caseSummary = caseSummary.First();
                    ViewBag.caseSummary = caseSummary.First();
                }
				if (policyDetailsViewModel.document_Master.Count() != 0)
                {
                    var bloodDocMaster = policyDetailsViewModel.document_Master.Where(x => x.DocType == "Medical");
                    if (bloodDocMaster.Count() != 0)
                    {
                        ViewBag.BloodTestPath = bloodDocMaster.First().Document_Path.Replace(@"\", @"/");
                    }

                    var ecgDocMaster = policyDetailsViewModel.document_Master.Where(x => x.DocType == "ECG");
                    if (ecgDocMaster.Count() != 0)
                    {
                        ViewBag.ECGPath = ecgDocMaster.First().Document_Path.Replace(@"\", @"/");
                    }

                    var financialDocMaster = policyDetailsViewModel.document_Master.Where(x => x.DocType == "Financial");
                    if (financialDocMaster.Count() != 0)
                    {
                        ViewBag.FinancialPath = financialDocMaster.First().Document_Path.Replace(@"\", @"/");
                    }

                    var ProposalDocMaster = policyDetailsViewModel.document_Master.Where(x => x.DocType == "Proposal");
                    if (ProposalDocMaster.Count() != 0)
                    {
                        ViewBag.ProposalPath = ProposalDocMaster.First().Document_Path.Replace(@"\", @"/");
                    }


                }
                ViewBag.Title = "Policy Details";
                ViewBag.isDataSaved = false;
                Session["PolicyNo"] = Convert.ToString(PolicyNo);
            }
            return policyDetailsViewModel;
        }

        public ActionResult ViewPolicyDetails(int? PolicyNo)
        {
            if (Session["PolicyNo"] != null)
                PolicyNo = PolicyNo == null ? Convert.ToInt32(Session["PolicyNo"]) : PolicyNo;
            return View(GetPolicyDetails(PolicyNo));
        }
        public ActionResult GetUpdatedFinanceData()
        {
            var updatedData = GetPolicyDetails(Convert.ToInt32(Session["PolicyNo"]));
            return PartialView("PartialViews/MonthlyFinance", updatedData.monthly_Finance_Master);
        }
        public ActionResult GetUpdatedFinanceData()
        {
            var updatedData = GetPolicyDetails(Convert.ToInt32(Session["PolicyNo"]));
            return PartialView("PartialViews/MonthlyFinance", updatedData.monthly_Finance_Master);
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        private List<Proposal> getProposal()
        {
            var list = new List<Proposal>();

            var proposal1 = new Proposal();
            proposal1.ProposalNo = 1;
            proposal1.ProposalDate = "02-06-2023";
            proposal1.ProposalStatus = "Pending";
            proposal1.ProposalDetail = "View";

            var proposal2 = new Proposal();
            proposal2.ProposalNo = 1;
            proposal2.ProposalDate = "02-06-2023";
            proposal2.ProposalStatus = "Pending";
            proposal2.ProposalDetail = "View";

            var proposal3 = new Proposal();
            proposal3.ProposalNo = 1;
            proposal3.ProposalDate = "02-06-2023";
            proposal3.ProposalStatus = "Pending";
            proposal3.ProposalDetail = "View";

            var proposal4 = new Proposal();
            proposal4.ProposalNo = 1;
            proposal4.ProposalDate = "02-06-2023";
            proposal4.ProposalStatus = "Pending";
            proposal4.ProposalDetail = "View";

            list.Add(proposal1);
            list.Add(proposal2);
            list.Add(proposal3);
            list.Add(proposal4);

            return list;
        }

        #region Http
        private static bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // Accept all certificates regardless of validation errors
            return true;
        }
        private static async Task<HttpResponseMessage> SendHttpRequest(string requestDataJson, string uri, int method)
        {
            try
            {
                using (var httpClient = new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = ValidateCertificate
                }))
                {
                    // Set the base URL of the Web API
                    httpClient.BaseAddress = new Uri(webApiUrl);

                    // Set the request content type
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // Create the request content
                    var content = new StringContent(requestDataJson, Encoding.UTF8, "application/json");

                    var response = new HttpResponseMessage();
                    if (method == Convert.ToInt32(Enums.HttpMethod.GET))
                    {
                        // Send the POST request and get the response
                        response = await httpClient.GetAsync(uri);
                    }
                    else if (method == Convert.ToInt32(Enums.HttpMethod.POST))
                    {
                        // Send the POST request and get the response
                        response = await httpClient.PostAsync(uri, content);

                    }
                    return response;
                }
            }
            catch (Exception ex)
            {
                //logger.Log("Error: " + ex.ToString());
                return null;
            }
        }
        #endregion
        public async Task<ActionResult> SaveMedicalReportDetails(List<Med_Report_Details> med_Report_Details)
        {
            var requestDataJson = JsonSerializer.Serialize(med_Report_Details);
            await SendHttpRequest(requestDataJson, "PolicyMasterAPI/EditMedicalReportValues", Convert.ToInt32(Enums.HttpMethod.POST));
            return RedirectToAction("ViewPolicyDetails");
        }
        public async Task<ActionResult> SaveECGReportDetails(Med_Report_Details med_Report_Details)
        {
            if (med_Report_Details.ReportDetailsID == 0)
            {
                int policyNo = Convert.ToInt32(Session["PolicyNo"]);
                var docMasterIDContent = await SendHttpRequest("", "PolicyMasterAPI/GetDocMasterIdForECG?policyNo=" + policyNo, Convert.ToInt32(Enums.HttpMethod.POST));
                var docMasterID = docMasterIDContent.Content.ReadAsStringAsync().Result;
                Med_Report_Master med_Report_Master = new Med_Report_Master()
                {
                    PolicyNo = policyNo,
                    DocMasterID = Convert.ToInt32(docMasterID),
                    ReportType = "ECG",
                    ReportDate = DateTime.Now,
                    Age = 20,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "SYSTEM"
                };
                var requestDataJson = JsonSerializer.Serialize(med_Report_Master);
                var med_Report_Master_Response = await SendHttpRequest(requestDataJson, "PolicyMasterAPI/AddMedicalReport", Convert.ToInt32(Enums.HttpMethod.POST));
                // Check if the response was successful
                if (med_Report_Master_Response.IsSuccessStatusCode)
                {
                    // Read the response content
                    var med_Report_Master_Content = await med_Report_Master_Response.Content.ReadAsStringAsync();
                    med_Report_Details.ReportID = Convert.ToInt32(med_Report_Master_Content);
                    med_Report_Details.CreatedDate = DateTime.Now;
                    med_Report_Details.CreatedBy = "SYSTEM";
                    requestDataJson = JsonSerializer.Serialize(med_Report_Details);
                    await SendHttpRequest(requestDataJson, "PolicyMasterAPI/AddECGReportValues", Convert.ToInt32(Enums.HttpMethod.POST));
                }
                else
                {
                    // Handle the error response
                    Console.WriteLine("Error: " + med_Report_Master_Response.StatusCode);
                }
            }
            else
            {
                var requestDataJson = JsonSerializer.Serialize(med_Report_Details);
                await SendHttpRequest(requestDataJson, "PolicyMasterAPI/EditECGReportValues", Convert.ToInt32(Enums.HttpMethod.POST));
            }
            return RedirectToAction("ViewPolicyDetails");
        }

        public async Task<ActionResult> SaveFinancialDetails(List<Monthly_Finance_Master> monthly_Finance_Master)
        {
            var requestDataJson = JsonSerializer.Serialize(monthly_Finance_Master);
            var httpResponse = await SendHttpRequest(requestDataJson, "PolicyMasterAPI/EditMonthly_FinancialValues", Convert.ToInt32(Enums.HttpMethod.POST));
            if (httpResponse.IsSuccessStatusCode)
            {
                ViewBag.isDataSaved = true;
                return Json(new { success = true, message = "Form data saved successfully." });
            }
            else
            {
                ViewBag.isDataSaved = false;
                return Json(new { success = false, message = "Error." });
            }
            //return RedirectToAction("ViewPolicyDetails");
        }

        public async Task<ActionResult> SaveProposalDetails(Proposal_Master proposal_Master)
        {
            var requestDataJson = JsonSerializer.Serialize(proposal_Master);
            await SendHttpRequest(requestDataJson, "PolicyMasterAPI/EditProposalValues", Convert.ToInt32(Enums.HttpMethod.POST));
            return RedirectToAction("ViewPolicyDetails");
        }


    }
}