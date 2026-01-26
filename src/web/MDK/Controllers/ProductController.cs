using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MDK.Models;
using Rotativa;

namespace MDK.Controllers
{
    public class ProductController : Controller
    {
        private Entities db = new Entities();
        public void loginkontrol()
        {
            if (Session["email"] == null)
            {
                Response.Redirect("/Auth/Login");
            }
        }

        public ActionResult Index_P()
        {
            loginkontrol();
            var products = db.Urun
                             .Include(u => u.Kategori)
                             .Where(c => c.Silindi == false)
                             .OrderBy(c => c.Id)
                             .ToList();
            return View("urun_listele_P", products);
        }

        public ActionResult ExportPdf_P()
        {
            var products = db.Urun
                             .Where(c => c.Silindi == false)
                             .OrderBy(c => c.Id)
                             .ToList();

            return new ViewAsPdf("urun_listele_P", products)
            {
                FileName = "UrunListesi.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Landscape,
                PageMargins = new Rotativa.Options.Margins { Top = 10, Bottom = 10, Left = 10, Right = 10 },
                CustomSwitches = "--user-style-sheet " + Server.MapPath("~/purple/excel-print.css")
            };
        }

        public ActionResult Details_P(int id)
        {
            loginkontrol();
            var urun = db.Urun.FirstOrDefault(c => c.Id == id);
            var receteler = db.Recete.Where(c => c.Urun_FK == id).ToList();
            var hammaddeListesi = new List<string>();
            var miktarListesi = new List<int>();
            var birimListesi = new List<string>();

            foreach (var recete in receteler)
            {
                var hammadde = db.Ham_Madde.FirstOrDefault(c => c.Id == recete.Ham_Madde_FK);
                hammaddeListesi.Add(hammadde.Ham_Madde1);
                miktarListesi.Add(Convert.ToInt32(recete.Miktar));

                if (recete.Ham_Madde.Birim.Birim1 == "Ağırlık")
                    birimListesi.Add("GR");
                else if (recete.Ham_Madde.Birim.Birim1 == "Uzunluk")
                    birimListesi.Add("CM");
                else if (recete.Ham_Madde.Birim.Birim1 == "Hacim")
                    birimListesi.Add("ML");
                else
                    birimListesi.Add("Adet");
            }

            if (urun == null)
            {
                return HttpNotFound("Product not found.");
            }

            ViewBag.Id = urun.Id;
            ViewBag.UrunAd = urun.Urun_Ad;
            ViewBag.Barkod = urun.Barkod;
            ViewBag.KategoriAd = urun.Kategori.Kategori1;
            ViewBag.BirimTip = urun.Birim.Birim1;
            ViewBag.HammaddeListesi = hammaddeListesi;
            ViewBag.BirimListesi = birimListesi;
            ViewBag.MiktarListesi = miktarListesi;
            ViewBag.Tanim = urun.Tanim;
            ViewBag.KaliteRaporu = urun.Kalite_Raporu;
            ViewBag.Kapasite = urun.Fabrika_Kapasitesi;

            return View("urun_detaylari_P");
        }

        // GET: Product/Create
        public ActionResult Create_P()
        {
            loginkontrol();
            var birimListesi = db.Birim.ToList();
            var hammaddeListesi = db.Ham_Madde.ToList();
            var kategoriListesi = db.Kategori.ToList();
            ViewBag.BirimListesi = birimListesi;
            ViewBag.HammaddeListesi = hammaddeListesi;
            ViewBag.KategoriListesi = kategoriListesi;

            return View("urun_ekle_P");
        }

        [HttpPost]
        public ActionResult Create_P(FormCollection collection, string[] HammaddeSelect, string[] Miktar)
        {
            loginkontrol();
            try
            {
                string name = collection.Get("productName");
                string barkodu = collection.Get("productBarkod");
                string tanimi = collection.Get("productTanim");
                string kalite_rapor = collection.Get("productKaliteRapor");
                string kapasite = collection.Get("productKapasite");
                string kategoris = collection.Get("KategoriSelect");
                string birimi = collection.Get("BirimSelect");

                Urun urunler = new Urun()
                {
                    Urun_Ad = name,
                    Kategori_FK = Convert.ToInt32(kategoris),
                    Birim_FK = Convert.ToInt32(birimi),
                    Barkod = barkodu,
                    Tanim = tanimi,
                    Kalite_Raporu = kalite_rapor,
                    Fabrika_Kapasitesi = Convert.ToInt32(kapasite),
                    Silindi = false
                };

                db.Urun.Add(urunler);
                db.SaveChanges();

                var enBuyukIdUrun = db.Urun.OrderByDescending(u => u.Id).FirstOrDefault();

                for (int i = 0; i < HammaddeSelect.Length; i++)
                {
                    int miktar = 0;
                    if (Miktar != null && i < Miktar.Length)
                    {
                        int.TryParse(Miktar[i], out miktar);

                        Recete recete1 = new Recete()
                        {
                            Ham_Madde_FK = Convert.ToInt32(HammaddeSelect[i]),
                            Miktar = miktar,
                            Urun_FK = enBuyukIdUrun.Id
                        };
                        db.Recete.Add(recete1);
                    }
                }

                db.SaveChanges();
                return RedirectToAction("Index_P");
            }
            catch
            {
                return View("urun_ekle_P");
            }
        }

        public ActionResult Edit_P(int id)
        {
            loginkontrol();
            var product = db.Urun
                .Include(u => u.Kategori)
                .Include(u => u.Birim)
                .SingleOrDefault(u => u.Id == id);

            if (product == null)
            {
                return HttpNotFound();
            }

            ViewBag.KategoriList = new SelectList(db.Kategori, "Id", "Kategori1", product.Kategori_FK);
            ViewBag.BirimList = new SelectList(db.Birim, "Id", "Birim1", product.Birim_FK);

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit_P(Urun model)
        {
            loginkontrol();
            if (ModelState.IsValid)
            {
                var product = db.Urun.Find(model.Id);
                if (product == null)
                {
                    return HttpNotFound();
                }

                product.Urun_Ad = model.Urun_Ad;
                product.Barkod = model.Barkod;
                product.Kategori_FK = model.Kategori_FK;
                product.Birim_FK = model.Birim_FK;
                product.Tanim = model.Tanim;
                product.Kalite_Raporu = model.Kalite_Raporu;
                product.Fabrika_Kapasitesi = model.Fabrika_Kapasitesi;

                db.SaveChanges();
                return RedirectToAction("Index_P");
            }

            ViewBag.KategoriList = new SelectList(db.Kategori, "Id", "Kategori1", model.Kategori_FK);
            ViewBag.BirimList = new SelectList(db.Birim, "Id", "Birim1", model.Birim_FK);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete_P(int id)
        {
            try
            {
                var recetes = db.Recete.Where(m => m.Urun_FK == id).ToList();
                if (recetes.Any())
                {
                    db.Recete.RemoveRange(recetes);
                    db.SaveChanges();
                }

                var urun = db.Urun.Find(id);
                if (urun != null)
                {
                    urun.Silindi = true;
                    db.SaveChanges();
                }

                return RedirectToAction("Index_P");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index_P");
            }
        }
    }
}