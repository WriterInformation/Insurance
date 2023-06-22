using System;
using BPaasInsurance.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BPaasInsurance.Controllers
{
    public class HomeController : Controller
    {
        //public ActionResult Index()
        //{
        //    var proposal = getProposal();

        //    return View(proposal);
        //}

        public ActionResult Index(int page = 1, string sort = "FirstName", string sortdir = "asc", string search = "")
        {
            string filePath = @"C:/Users/shuheb.shaikh/Documents/10136418552906001.pdf";
            string url = Url.Action("ViewDocument", "Document", new { filePath });
            ViewBag.Doc = url;


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

        public ActionResult ViewDocument(string filePath)
        {
            // Ensure the filePath is valid and exists
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                return HttpNotFound(); // Or any other appropriate error response
            }

            // Read the file contents and determine the content type
            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            string contentType = MimeMapping.GetMimeMapping(filePath);

            // Return the file as a FileResult
            return File(fileBytes, contentType);
        }



        public ActionResult ViewPolicyDetails(int? PolicyNo)
        {
            using (InsuranceBPaaSEntities dc = new InsuranceBPaaSEntities())
            {
                var v = (from a in dc.Med_Report_Details
                         where
                                 a.Med_Report_Master.PolicyNo.ToString().Contains(PolicyNo.ToString())
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
                PolicyDetailsViewModel policyDetailsViewModel = new PolicyDetailsViewModel();
                policyDetailsViewModel.med_Report_Details = v.ToList();
                policyDetailsViewModel.document_Master = doc.ToList();
                ViewBag.Title = "Policy Details";
                return View(policyDetailsViewModel);
            }

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

        
    }
}