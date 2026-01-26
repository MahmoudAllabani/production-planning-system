using MDK.Models;
using Rotativa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MDK.Controllers
{
    public class StockController : Controller
    {
        private Entities db = new Entities();
        // GET: Stock
        public void loginkontrol()
        {
            if (Session["email"] == null)
            {
                Response.Redirect("/Auth/Login");
            }
        }
        public ActionResult Index_S()
        {
            loginkontrol();
            List<Stok> stock = db.Stok.OrderBy(c => c.Id).ToList();
            return View(stock);
        }

        public ActionResult ExportPdf()
        {
            var s = db.Stok
                .Where(c => c.Miktar != null)
                             .OrderBy(c => c.Id)
                             .ToList();

            return new ViewAsPdf("Index_S", s)
            {
                FileName = "StokListesi.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Landscape,
                PageMargins = new Rotativa.Options.Margins { Top = 10, Bottom = 10, Left = 10, Right = 10 },
                CustomSwitches = "--user-style-sheet " + Server.MapPath("~/purple/excel-print.css")
            };
        }
        public ActionResult IndexError_S()
        {
            loginkontrol();
            List<Stok> stock = db.Stok.OrderBy(c => c.Id).ToList();
            return View(stock);
        }

        public ActionResult Create_S()
        {
            loginkontrol();
            var hammaddeler = db.Ham_Madde.OrderBy(h => h.Ham_Madde1).ToList();
            return View(hammaddeler);
        }

        [HttpPost]
        public ActionResult Create_S(FormCollection collection)
        {
            loginkontrol();
            string miktar = collection.Get("stockmiktar");
            string hammadde_isim = collection.Get("stockhammadde");

            var hammadde = db.Ham_Madde.FirstOrDefault(h => h.Ham_Madde1 == hammadde_isim);

            if (hammadde != null)
            {
                int hammadde_id = hammadde.Id;

                var stok = db.Stok.FirstOrDefault(h => h.Ham_Madde_FK == hammadde_id);

                if (stok != null)
                {
                    stok.Miktar += Convert.ToInt32(miktar);
                    db.SaveChanges();
                }
                else
                {
                    Stok stok1 = new Stok()
                    {
                        Miktar = Convert.ToInt32(miktar),
                        Ham_Madde_FK = hammadde_id
                    };

                    db.Stok.Add(stok1);
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Index_S");
        }
        // POST: Stock/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete_S(int id, FormCollection collection)
        {
            loginkontrol();
            var stok = db.Stok.FirstOrDefault(m => m.Id == id);
            // Hedef hammadde başka yerde kullanılıyor mu kontrol et
            int yeniHammaddeId = Convert.ToInt32(collection["Ham_Madde_FK"]);
            bool kullaniliyorMu = db.Recete.Any(r => r.Ham_Madde_FK == yeniHammaddeId);
            if (kullaniliyorMu)
            {
                return RedirectToAction("IndexError_S");
            }
            else
            {
                if (stok != null)
                {
                    stok.Miktar = 0; // veya db.Stok.Remove(stok); // eğer fiziksel silme istiyorsan
                    db.SaveChanges();
                }

                return RedirectToAction("Index_S");
            }

        }
        public ActionResult Edit_S(int id)
        {
            loginkontrol();
            var stok = db.Stok.FirstOrDefault(s => s.Id == id);
            if (stok == null)
            {
                return HttpNotFound();
            }

            ViewBag.Hammaddeler = new SelectList(db.Ham_Madde.ToList(), "Id", "Ham_Madde1", stok.Ham_Madde_FK);
            return View(stok);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit_S(int id, FormCollection collection)
        {
            loginkontrol();
            var mevcutStok = db.Stok.FirstOrDefault(s => s.Id == id);
            if (mevcutStok == null)
                return HttpNotFound();

            try
            {
                int yeniHammaddeId = Convert.ToInt32(collection["Ham_Madde_FK"]);
                int yeniMiktar = Convert.ToInt32(collection["Miktar"]);

                // Eğer kullanıcı aynı hammaddeyle güncelleme yapıyorsa sadece miktarı değiştir
                if (mevcutStok.Ham_Madde_FK == yeniHammaddeId)
                {
                    mevcutStok.Miktar = yeniMiktar;
                }
                else
                {
                    // Hedef hammadde başka yerde kullanılıyor mu kontrol et
                    bool kullaniliyorMu = db.Recete.Any(r => r.Ham_Madde_FK == yeniHammaddeId);
                    if (kullaniliyorMu)
                    {
                        return RedirectToAction("IndexError_S");
                    }

                    // Yeni hammaddeye ait stok var mı?
                    var hedefStok = db.Stok.FirstOrDefault(s => s.Ham_Madde_FK == yeniHammaddeId);
                    if (hedefStok != null)
                    {
                        // Hedef stok varsa miktarı birleştir, mevcut stoğu sil
                        hedefStok.Miktar += yeniMiktar;
                        db.Stok.Remove(mevcutStok);
                    }
                    else
                    {
                        // Yoksa güncelle
                        mevcutStok.Ham_Madde_FK = yeniHammaddeId;
                        mevcutStok.Miktar = yeniMiktar;
                    }
                }

                db.SaveChanges();
                return RedirectToAction("Index_S");
            }
            catch
            {
                TempData["Hata"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction("Edit_S", new { id = id });
            }
        }

    }
} 