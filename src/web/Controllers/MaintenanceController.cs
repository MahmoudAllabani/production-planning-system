using MDK.Models;
using Rotativa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MDK.Controllers
{
    public class MaintenanceController : Controller
    {
        private Entities db = new Entities();
        public void loginkontrol()
        {
            if (Session["email"] == null)
            {
                Response.Redirect("/Auth/Login");
            }
        }
        public ActionResult Index_BK()
        {
            loginkontrol();
            List<Bakim> maintenanceList = db.Bakim.OrderBy(b => b.Id).ToList();
            return View("Index_BK", maintenanceList);
        }

        // Export to PDF
        public ActionResult ExportPdf_BK()
        {
            var bakimList = db.Bakim.OrderBy(b => b.Id).ToList();

            return new ViewAsPdf("Index_BK", bakimList)
            {
                FileName = "BakimListele.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Landscape,
                PageMargins = new Rotativa.Options.Margins { Top = 10, Bottom = 10, Left = 10, Right = 10 },
                CustomSwitches = "--user-style-sheet " + Server.MapPath("~/purple/excel-print.css")
            };
        }

        // GET: Maintenance/Details/5
        public ActionResult Details_BK(int id)
        {
            loginkontrol();
            var maintenance = db.Bakim.FirstOrDefault(b => b.Id == id);
            if (maintenance == null)
            {
                return HttpNotFound();
            }
            return View("Details_BK", maintenance);
        }

        // GET: Maintenance/Create
        public ActionResult Create_BK()
        {
            loginkontrol();
            return View("Create_BK");
        }

        // POST: Maintenance/Create
        [HttpPost]
        public ActionResult Create_BK(FormCollection collection)
        {
            loginkontrol();
            try
            {
                Bakim bakim = new Bakim()
                {
                    Ad = collection.Get("Ad"),
                    Baslangic_Tarihi = DateTime.Parse(collection.Get("Baslangic_Tarihi")),
                    Bakim_Suresi = int.Parse(collection.Get("Bakim_Suresi")),
                    Bakim_Nedeni = collection.Get("Bakim_Nedeni")
                };

                var bakimlar = db.Bakim.OrderByDescending(u => u.Id).ToList();

                // Calculate end date by adding minutes to start date
                bakim.Bitis_Tarihi = bakim.Baslangic_Tarihi.Value.AddMinutes((double)bakim.Bakim_Suresi.Value);

                foreach (var maintenance in bakimlar)
                {
                    if (bakim.Baslangic_Tarihi == maintenance.Baslangic_Tarihi)
                    {
                        if (bakim.Bitis_Tarihi == maintenance.Bitis_Tarihi)
                        {
                            TempData["ErrorMessage"] = "Bu saatte zaten planlanmış bir bakım var.";
                            return RedirectToAction("Create_BK");
                        }
                        else if(bakim.Bitis_Tarihi > maintenance.Bitis_Tarihi)
                        {
                            bakim.Baslangic_Tarihi = maintenance.Bitis_Tarihi;
                        }
                    }
                    else if(bakim.Baslangic_Tarihi >= maintenance.Baslangic_Tarihi && bakim.Bitis_Tarihi <= maintenance.Bitis_Tarihi)
                    {
                        TempData["ErrorMessage"] = "Bu saatte zaten planlanmış bir bakım var.";
                        return RedirectToAction("Create_BK");
                    }
                    else if (bakim.Baslangic_Tarihi > maintenance.Baslangic_Tarihi && bakim.Baslangic_Tarihi <= maintenance.Bitis_Tarihi && bakim.Bitis_Tarihi > maintenance.Bitis_Tarihi)
                    {
                        bakim.Baslangic_Tarihi = maintenance.Bitis_Tarihi;
                    }
                    else if (bakim.Baslangic_Tarihi < maintenance.Baslangic_Tarihi && bakim.Bitis_Tarihi > maintenance.Baslangic_Tarihi && bakim.Bitis_Tarihi <= maintenance.Bitis_Tarihi)
                    {
                        bakim.Bitis_Tarihi = maintenance.Baslangic_Tarihi;
                    }
                    else if (bakim.Baslangic_Tarihi < maintenance.Baslangic_Tarihi && bakim.Bitis_Tarihi > maintenance.Bitis_Tarihi)
                    {
                        TempData["ErrorMessage"] = "Bu saatte zaten planlanmış bir bakım var.";
                        return RedirectToAction("Create_BK");
                    }
                }

                db.Bakim.Add(bakim);
                db.SaveChanges();

                return RedirectToAction("Index_BK");
            }
            catch (Exception ex)
            {
                // Log the error
                return View("Create_BK");
            }
        }

        // GET: Maintenance/Edit/5
        public ActionResult Edit_BK(int id)
        {
            loginkontrol();
            var maintenance = db.Bakim.FirstOrDefault(b => b.Id == id);
            if (maintenance == null)
            {
                return HttpNotFound();
            }
            return View("Edit_BK", maintenance);
        }

        [HttpPost]
        public ActionResult Edit_BK(int id, FormCollection collection)
        {
            loginkontrol();
            try
            {
                var maintenance = db.Bakim.FirstOrDefault(b => b.Id == id);
                if (maintenance == null)
                {
                    return HttpNotFound();
                }

                maintenance.Ad = collection.Get("Ad");
                maintenance.Baslangic_Tarihi = DateTime.Parse(collection.Get("Baslangic_Tarihi"));
                maintenance.Bakim_Suresi = int.Parse(collection.Get("Bakim_Suresi"));
                maintenance.Bakim_Nedeni = collection.Get("Bakim_Nedeni");
                maintenance.Bitis_Tarihi = DateTime.Parse(collection.Get("Baslangic_Tarihi"));

                // Calculate end date by adding minutes to start date
                maintenance.Bitis_Tarihi = maintenance.Baslangic_Tarihi.Value.AddMinutes((double)maintenance.Bakim_Suresi.Value);

                db.SaveChanges();

                return RedirectToAction("Index_BK");
            }
            catch (Exception ex)
            {
                // Log the error
                return View("Edit_BK");
            }
        }

        // GET: Maintenance/Delete/5
        public ActionResult Delete_BK(int id)
        {
            var bakim = db.Bakim.FirstOrDefault(b => b.Id == id);
            if (bakim != null)
            {
                db.Bakim.Remove(bakim);
                db.SaveChanges();
            }
            return RedirectToAction("Index_BK");
        }

        [HttpPost]
        public ActionResult Delete_BK(int id, FormCollection collection)
        {
            try
            {
                return RedirectToAction("Index_BK");
            }
            catch
            {
                return View("Index_BK");
            }
        }
    }
}