using MDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Globalization;

namespace MDK.Controllers
{
    public class HomeController : Controller
    {
        private Entities db = new Entities();
        // GET: Home
        public void loginkontrol()
        {
            if (Session["email"] == null)
            {
                Response.Redirect("/Auth/Login");
            }
        }
        public ActionResult Index()
        {
            loginkontrol();
            //int musteriSayisi = db.Musteri.Where(c => c.Silindi == false).Count();
            //int urunSayisi = db.Urun.Where(c => c.Silindi == false).Count();
            //int kategoriSayisi = db.Kategori.Where(c => c.Silindi == false).Count();
            //int personelSayisi = db.Personel.Where(c => c.Silindi == false).Count();
            //int ham_Turu = db.Ham_Madde.Where(c => c.Silindi == false).Count();
            //int siparisSayisi = db.Siparis.Where(c => c.Silindi == false).Count();
            int musteriSayisi = db.Musteri.Count();
            int urunSayisi = db.Urun.Count();
            int kategoriSayisi = db.Kategori.Count();
            int personelSayisi = db.Personel.Count();
            var ham_Turu = db.Ham_Madde.Count();
            var siparisSayisi = db.Siparis.Count();

            var ortalamaMaas = db.Personel.Average(p => p.Maas);
            //var ortalamaMaas = db.Personel.Average(p => p.Maas) ?? 0;
            if (ortalamaMaas == null)
            {
                ortalamaMaas = 0;
            }
            else
            {
                ortalamaMaas = Math.Round((double)ortalamaMaas, 2);
            }
            string formatliOrtalamaMaas = string.Format("{0:n0}", ortalamaMaas.ToString()).Replace(",", ".");
            //string formatliOrtalamaMaas = string.Format("{0:#,#}", ortalamaMaas).Replace(",", ".");

            int fabrikaKapasitesi = Convert.ToInt32(db.Ayarlar.FirstOrDefault(a => a.Ad == "fabrika_kapasite")?.Deger);

            string formatliKapasite = string.Format("{0:#,#}", fabrikaKapasitesi).Replace(",", ".");

            var son5Musteri = db.Musteri.Where(c => c.Silindi == false).OrderByDescending(m => m.Id).Take(5).ToList();
            var son5Urun = db.Urun.Where(c => c.Silindi == false).OrderByDescending(u => u.Id).Take(5).ToList();

            var son5Siparis = db.Siparis.Where(c => c.Silindi == false).OrderByDescending(s => s.Id).Take(5).ToList();

            DateTime bugun = DateTime.Today;
            var yeniSiparisler = db.Siparis.Where(s => s.Teslim_Tarihi > bugun);

            int fabrikaKaplananKapasite = 0;

            foreach (var siparis in yeniSiparisler)
            {

                if (siparis.Urun != null && siparis.Urun.Fabrika_Kapasitesi.HasValue)
                {
                    fabrikaKaplananKapasite += siparis.Urun.Fabrika_Kapasitesi.Value * siparis.Miktar.Value;
                }
            }

            int fabrikaKalanKapasitesi = fabrikaKapasitesi - fabrikaKaplananKapasite;

            ViewBag.MusteriSayisi = musteriSayisi;
            ViewBag.UrunSayisi = urunSayisi;
            ViewBag.KategoriSayisi = kategoriSayisi;
            ViewBag.PersonelSayisi = personelSayisi;
            ViewBag.HamMaddeSayisi = ham_Turu;
            ViewBag.SiparisSayisi = siparisSayisi;

            ViewBag.FabrikaKapasitesi = formatliKapasite;

            ViewBag.Son5Musteri = son5Musteri;
            ViewBag.Son5Urun = son5Urun;
            ViewBag.Son5Siparis = son5Siparis;

            ViewBag.OrtalamaMaas = formatliOrtalamaMaas;

            return View();
        }
        public ActionResult Details(int id)
        {
            return View();
        }

        public ActionResult Index1()
        {
            return View();
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public ActionResult Edit(int id)
        {
            return View();
        }

        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public ActionResult Delete(int id)
        {
            return View();
        }

        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}