using MDK.Models;
using Rotativa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;


namespace MDK.Controllers
{
    public class MaterialController : Controller
    {
        private Entities db = new Entities();
        public void loginkontrol()
        {
            if (Session["email"] == null)
            {
                Response.Redirect("/Auth/Login");
            }
        }
        public ActionResult Index_M()
        {
            loginkontrol();
            List<Ham_Madde> ham_madde = db.Ham_Madde.Include("Birim").ToList();
            return View("Index_M", ham_madde);
        }

        public ActionResult ExportPdf()
        {
            var m = db.Ham_Madde
                .Where(c => c.Ham_Madde1 != null)
                             .OrderBy(c => c.Id)
                             .ToList();

            return new ViewAsPdf("Index_M", m)
            {
                FileName = "HamMaddeListesi.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Landscape,
                PageMargins = new Rotativa.Options.Margins { Top = 10, Bottom = 10, Left = 10, Right = 10 },
                CustomSwitches = "--user-style-sheet " + Server.MapPath("~/purple/excel-print.css")
            };
        }
        public ActionResult Create_M()
        {
            loginkontrol();
            List<Birim> unit = db.Birim.ToList();
            return View("Create_M", unit);
        }



        [HttpPost]
        public ActionResult Create_M(FormCollection collection)
        {
            loginkontrol();
            string hammade = collection.Get("HamMaddeAdi");
            int hammade_birim = Convert.ToInt32(collection.Get("Birim"));
            // Aynı isimde hammadde var mı kontrol et
            bool exists = db.Ham_Madde.Any(h => h.Ham_Madde1 == hammade && h.Birim_FK == hammade_birim);


            if (exists)
            {
                // Kullanıcıya hata mesajı göster
                TempData["Error"] = "Bu hammadde zaten mevcut.";
                List<Birim> unit = db.Birim.ToList();
                return View("Create_M", unit);
            }
            else
            {
                Ham_Madde a = new Ham_Madde()
                {
                    Ham_Madde1 = hammade,
                    Birim_FK = hammade_birim
                };
                db.Ham_Madde.Add(a);
                db.SaveChanges();
                TempData["Success"] = true;
                return RedirectToAction("Index_M");
            }

        }


        // POST: Material/Create
        //[HttpPost]
        //public ActionResult Create_M(FormCollection collection)
        //{
        //    string hammade = collection.Get("HamMaddeAdi");
        //    int hammade_birim = Convert.ToInt32(collection.Get("Birim"));

        //    Ham_Madde a = new Ham_Madde()
        //    {
        //        Ham_Madde1 = hammade,
        //        Birim_FK = hammade_birim
        //    };
        //    db.Ham_Madde.Add(a);
        //    db.SaveChanges();
        //    return RedirectToAction("/Material/Index_M");
        //}
        public ActionResult IndexError_M()
        {
            loginkontrol();
            List<Ham_Madde> a = db.Ham_Madde.ToList();
            return View("IndexError_M", a);
        }
        // GET: Material/Delete/1
        public ActionResult Delete_M()
        {
            return View();
        }

        // POST: Material/Delete/1
        [HttpPost]
        public ActionResult Delete_M(int id)
        {

            var Ham = db.Ham_Madde.FirstOrDefault(m => m.Id == id);
            bool hasMatchingRecete = db.Recete.Any(recete => recete.Ham_Madde_FK == id);

            if (Ham != null && !hasMatchingRecete)
            {
                db.Ham_Madde.Remove(Ham);
                db.SaveChanges();
                return RedirectToAction("Index_M");
            }
            else
            {
                return RedirectToAction("IndexError_M");
            }
        }
        // GET: Material/Edit/1
        public ActionResult Edit_M(int id)
        {
            loginkontrol();
            var hamMadde = db.Ham_Madde.FirstOrDefault(x => x.Id == id);
            if (hamMadde == null)
            {
                return HttpNotFound();
            }
            List<Birim> birimler = db.Birim.ToList();
            ViewBag.Birimler = birimler;
            return View(hamMadde);
        }
        // POST: Material/Edit/1
        [HttpPost]
        public ActionResult Edit_M(Ham_Madde hamMadde)
        {
            loginkontrol();
            try
            {
                var entity = db.Ham_Madde.FirstOrDefault(x => x.Id == hamMadde.Id);
                if (entity == null)
                {
                    return HttpNotFound();
                }

                entity.Ham_Madde1 = hamMadde.Ham_Madde1;
                entity.Birim_FK = hamMadde.Birim_FK;

                db.SaveChanges();

                return RedirectToAction("Index_M");
            }
            catch
            {
                return View();
            }
        }
    }
}