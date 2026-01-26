using System;
using System.Web.Mvc;
using MLPrediction.Services;
using System.IO;
using System.Linq;
using MDK.Models;
using System.Data.Entity;
using System.Text;

namespace MDK.Controllers
{
    public class PredictionController : Controller
    {
        private Entities db = new Entities();

        private readonly string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "DemandPredictionModel.zip");

        public void loginkontrol()
        {
            if (Session["email"] == null)
            {
                Response.Redirect("/Auth/Login");
            }
        }
        // GET: Prediction/Train
        // Sadece sayfa yüklenir, model eğitilmez
        // public ActionResult Train()
        // {
        //     return View();
        // }

        // POST: Prediction/Train
        // [HttpPost]
        // public ActionResult TrainModel()
        // {
        //     try
        //     {
        //         var result = ModelTrainer.TrainAndSaveModel(modelPath);
        //         ViewBag.Message = "Model training completed successfully! R²: " + (result.Metrics?.RSquared.ToString("P2") ?? "-");
        //     }
        //     catch (Exception ex)
        //     {
        //         ViewBag.Message = "Error during model training: " + ex.Message;
        //     }
        //     return View("Train");
        // }

        // GET: Prediction/Predict
        public ActionResult Predict()
        {
            loginkontrol();

            var products = db.Urun.Where(u => u.Silindi == false)
                                  .Select(u => new { u.Id, u.Urun_Ad })
                                  .ToList();
            ViewBag.Products = new SelectList(products, "Id", "Urun_Ad");
            return View();
        }

        // POST: Prediction/Predict
        [HttpPost]
        public ActionResult Predict(int urunId, DateTime tarih)
        {
            loginkontrol();
            var products = db.Urun.Where(u => u.Silindi == false)
                                  .Select(u => new { u.Id, u.Urun_Ad })
                                  .ToList();
            ViewBag.Products = new SelectList(products, "Id", "Urun_Ad", urunId);
            try
            {
                using (var predictor = new DemandPredictor(modelPath))
                {
                    var prediction = predictor.Predict(urunId, tarih);
                    ViewBag.Prediction = prediction;

                    // Tahmin yapıldıktan hemen sonra ekle                    
                    var tahmin = new Talep_Tahmini
                    {
                        Urun_FK = urunId,
                        Tahmin_Tarihi = tarih,
                        Tahmin_Miktari = prediction
                    };
                    db.Talep_Tahmini.Add(tahmin);
                    db.SaveChanges();

                    var urun = db.Urun.FirstOrDefault(u => u.Id == urunId);
                    ViewBag.Birim = urun.Birim.Birim1;
                    // --- Stok ve durum hesaplama ---
                    // Stok tablosunda ürün ile bağlantı Ham_Madde_FK üzerinden, ancak burada urunId bir ürün id'si.
                    // Eğer ürünün hammaddesi ile stok ilişkisi varsa, ilgili Ham_Madde_FK'ya göre sorgulama yapılmalı.
                    // Örneğin, ürünün reçetesindeki ilk hammaddeye göre stok kontrolü yapılabilir.

                    // Ürünün reçetesindeki ilk hammaddeyi bul
                    var ilkRecete = db.Recete.FirstOrDefault(r => r.Urun_FK == urunId);
                    Stok stok = null;
                    double mevcutStok = 0;
                    if (ilkRecete != null)
                    {
                        stok = db.Stok.FirstOrDefault(s => s.Ham_Madde_FK == ilkRecete.Ham_Madde_FK);
                        mevcutStok = stok != null && stok.Miktar.HasValue ? stok.Miktar.Value : 0;
                    }
                    ViewBag.CurrentStock = mevcutStok;
                    if (mevcutStok >= prediction)
                    {
                        ViewBag.IsStockSufficient = true;
                        ViewBag.StockStatus = "Yeterli";
                    }
                    else
                    {
                        ViewBag.IsStockSufficient = false;
                        ViewBag.StockStatus = "Yetersiz";
                    }
                    ViewBag.ProductName = urun != null ? urun.Urun_Ad : "";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Tahmin sırasında hata: " + ex.Message;
            }
            return View();
        }

        public ActionResult Predictions()
        {
            loginkontrol();
            var predictions = db.Talep_Tahmini
                .Include(t => t.Urun)
                .OrderByDescending(x => x.Id)
                .ToList();
            return View(predictions);
        }
        [HttpGet]
        public ActionResult Delete(int id)
        {
            var tahmin = db.Talep_Tahmini.Find(id);
            if (tahmin != null)
            {
                db.Talep_Tahmini.Remove(tahmin);
                db.SaveChanges();
            }
            return RedirectToAction("Predictions");
        }
    }
}