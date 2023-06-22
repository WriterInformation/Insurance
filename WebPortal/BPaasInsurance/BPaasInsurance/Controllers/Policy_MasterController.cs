using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BPaasInsurance;

namespace BPaasInsurance.Controllers
{
    public class Policy_MasterController : Controller
    {
        private InsuranceBPaaSEntities db = new InsuranceBPaaSEntities();

        // GET: Policy_Master
        public ActionResult Index()
        {
            var policy_Master = db.Policy_Master.Include(p => p.Type_Master).Include(p => p.Type_Master);
            return View(policy_Master.ToList());
        }

        // GET: Policy_Master/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Policy_Master policy_Master = db.Policy_Master.Find(id);
            if (policy_Master == null)
            {
                return HttpNotFound();
            }
            return View(policy_Master);
        }

        // GET: Policy_Master/Create
        public ActionResult Create()
        {
            ViewBag.PolicyStatusCode = new SelectList(db.Type_Master, "TypeId", "TypeName");
            ViewBag.PolicyStatusCode = new SelectList(db.Type_Master, "TypeId", "TypeName");
            return View();
        }

        // POST: Policy_Master/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "PolicyNo,PolicyStatusCode,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy")] Policy_Master policy_Master)
        {
            if (ModelState.IsValid)
            {
                db.Policy_Master.Add(policy_Master);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.PolicyStatusCode = new SelectList(db.Type_Master, "TypeId", "TypeName", policy_Master.PolicyStatusCode);
            ViewBag.PolicyStatusCode = new SelectList(db.Type_Master, "TypeId", "TypeName", policy_Master.PolicyStatusCode);
            return View(policy_Master);
        }

        // GET: Policy_Master/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Policy_Master policy_Master = db.Policy_Master.Find(id);
            if (policy_Master == null)
            {
                return HttpNotFound();
            }
            ViewBag.PolicyStatusCode = new SelectList(db.Type_Master, "TypeId", "TypeName", policy_Master.PolicyStatusCode);
            ViewBag.PolicyStatusCode = new SelectList(db.Type_Master, "TypeId", "TypeName", policy_Master.PolicyStatusCode);
            return View(policy_Master);
        }

        // POST: Policy_Master/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "PolicyNo,PolicyStatusCode,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy")] Policy_Master policy_Master)
        {
            if (ModelState.IsValid)
            {
                db.Entry(policy_Master).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.PolicyStatusCode = new SelectList(db.Type_Master, "TypeId", "TypeName", policy_Master.PolicyStatusCode);
            ViewBag.PolicyStatusCode = new SelectList(db.Type_Master, "TypeId", "TypeName", policy_Master.PolicyStatusCode);
            return View(policy_Master);
        }

        // GET: Policy_Master/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Policy_Master policy_Master = db.Policy_Master.Find(id);
            if (policy_Master == null)
            {
                return HttpNotFound();
            }
            return View(policy_Master);
        }

        // POST: Policy_Master/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Policy_Master policy_Master = db.Policy_Master.Find(id);
            db.Policy_Master.Remove(policy_Master);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
