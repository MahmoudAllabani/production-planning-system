using MDK.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Data;
using Rotativa;

namespace MDK.Controllers
{
    public class CategoryController : Controller
    {
        private Entities db = new Entities();
        public void loginkontrol()
        {
            if (Session["email"] == null)
            {
                Response.Redirect("/Auth/Login");
            }
        }

        // GET: Category
        public ActionResult Index_C()
        {
            loginkontrol();
            return View(db.Kategori.ToList());
        }

        public ActionResult ExportPdf_C()
        {
            var categories = db.Kategori
                               .OrderBy(c => c.Id)
                               .ToList(); 

            return new ViewAsPdf("Index_C", categories)
            {
                FileName = "CategoryListesi.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Landscape,
                PageMargins = new Rotativa.Options.Margins { Top = 10, Bottom = 10, Left = 10, Right = 10 },
                CustomSwitches = "--user-style-sheet " + Server.MapPath("~/purple/excel-print.css")
            };
        }


        // GET: Category/Details/5
        public ActionResult Details_C(int? id)
        {
            loginkontrol();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Kategori kategori = db.Kategori.Find(id);
            if (kategori == null)
            {
                return HttpNotFound();
            }
            return View(kategori);
        }

        // GET: Category/Create
        public ActionResult Create_C()
        {
            loginkontrol();
            return View();
        }

        // POST: Category/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create_C([Bind(Include = "Id,Kategori1")] Kategori kategori)
        {
            loginkontrol();
            if (ModelState.IsValid)
            {
                db.Kategori.Add(kategori);
                db.SaveChanges();
                return RedirectToAction("Index_C");
            }

            return View(kategori);
        }

        // GET: Category/Edit/5
        public ActionResult Edit_C(int? id)
        {

            loginkontrol();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Kategori kategori = db.Kategori.Find(id);
            if (kategori == null)
            {
                return HttpNotFound();
            }
            return View(kategori);
        }

        // POST: Category/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit_C([Bind(Include = "Id,Kategori1")] Kategori kategori)
        {
            if (ModelState.IsValid)
            {
                db.Entry(kategori).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index_C");
            }
            return View(kategori);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete_C(int id)
        {
            var urunler = db.Urun.Where(u => u.Kategori_FK == id).ToList();

            if (urunler.Any())
            {
                var urunIds = urunler.Select(u => u.Id).ToList();
                db.Recete.RemoveRange(db.Recete.Where(r => urunIds.Contains(r.Urun_FK ?? 0)));
                db.Urun.RemoveRange(urunler);
            }

            var kategori = db.Kategori.Find(id);
            if (kategori != null)
            {
                db.Kategori.Remove(kategori);
            }
            db.SaveChanges();
            return RedirectToAction("Index_C");
        }


    }
}