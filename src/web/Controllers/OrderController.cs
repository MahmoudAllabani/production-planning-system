using MDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Rotativa;

namespace MDK.Controllers
{
    public class OrderController : Controller
    {
        private Entities db = new Entities();


        // GET: Order
        public void loginkontrol()
        {
            if (Session["email"] == null)
            {
                Response.Redirect("/Auth/Login");
            }
        }

        public ActionResult Index_SI()
        {
            loginkontrol();
            List<Siparis> orders = db.Siparis
                .Include(s => s.Musteri)
                .Include(s => s.Urun)
                .Where(s => s.Silindi == false)
                .Where(s => s.Teslim_Tarihi > DateTime.Now)
                .OrderByDescending(c => c.Id)
                .ToList();
            return View("Index_SI", orders);
        }

        public ActionResult ExportPdf_SI()
        {
            var siparisList = db.Siparis.OrderBy(b => b.Id).ToList();

            return new ViewAsPdf("Index_SI", siparisList)
            {
                FileName = "SiparisListele.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Landscape,
                PageMargins = new Rotativa.Options.Margins { Top = 10, Bottom = 10, Left = 10, Right = 10 },
                CustomSwitches = "--user-style-sheet " + Server.MapPath("~/purple/excel-print.css")
            };
        }

        public ActionResult Siparis_SI()
        {
            loginkontrol();
            var musteriler = db.Musteri.Where(c => c.Silindi == false).OrderByDescending(m => m.Id).ToList();
            var urunler = db.Urun.Where(c => c.Silindi == false).OrderByDescending(u => u.Id).ToList();

            var ilkbirim = db.Urun.Where(c => c.Silindi == false).OrderByDescending(u => u.Id).FirstOrDefault()?.Birim?.Birim1;

            ViewBag.customers = musteriler;
            ViewBag.products = urunler;
            ViewBag.birimoffirst = ilkbirim;

            return View("Siparis_SI");
        }

        public ActionResult Deleted_SI()
        {
            List<Siparis> orders = db.Siparis
                .Include(s => s.Musteri)
                .Include(s => s.Urun)
                .Where(s => s.Silindi == true)
                .OrderByDescending(c => c.Id)
                .ToList();
            return View("Deleted_SI", orders);
        }

        public ActionResult Completed_SI()
        {
            loginkontrol();
            List<Siparis> orders = db.Siparis
                .Include(s => s.Musteri)
                .Include(s => s.Urun)
                .Where(s => s.Silindi == false)
                .Where(s => s.Teslim_Tarihi < DateTime.Now)
                .OrderByDescending(c => c.Id)
                .ToList();
            return View("Completed_SI", orders);
        }

        // GET: Order/Details/5
        public ActionResult Details_SI(int id)
        {
            loginkontrol();
            var order = db.Siparis
                .Include(s => s.Musteri)
                .Include(s => s.Urun)
                .FirstOrDefault(s => s.Id == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // GET: Order/Create
        public ActionResult Create_SI()
        {
            loginkontrol();
            var musteriler = db.Musteri.Where(c => c.Silindi == false).OrderByDescending(m => m.Id).ToList();
            var urunler = db.Urun.Where(c => c.Silindi == false).OrderByDescending(u => u.Id).ToList();

            var productDisplayNames = new Dictionary<int, string>();
            foreach (var urun in urunler)
            {
                var receteler = db.Recete.Where(r => r.Urun_FK == urun.Id).ToList();
                var hammaddeler = receteler.Select(recete =>
                {
                    var hammadde = db.Ham_Madde.FirstOrDefault(h => h.Id == recete.Ham_Madde_FK);
                    return $"{recete.Miktar} {hammadde?.Ham_Madde1}";
                }).ToList();
                productDisplayNames[urun.Id] = $"{urun.Urun_Ad} ({string.Join(", ", hammaddeler)})";
            }

            ViewBag.customers = musteriler;
            ViewBag.products = urunler;
            ViewBag.productDisplayNames = productDisplayNames;

            if (TempData["FormData"] is Dictionary<string, object> formData)
            {
                ViewBag.SelectedCustomerId = formData.ContainsKey("musteri") ? (int)formData["musteri"] : 0;
                ViewBag.SelectedUrunId = formData.ContainsKey("urun") ? (int)formData["urun"] : 0;
                ViewBag.SelectedMiktar = formData.ContainsKey("miktar") ? (int)formData["miktar"] : 0;
                ViewBag.SelectedMesai = formData.ContainsKey("mesai") ? (int)formData["mesai"] : 0;
                ViewBag.SelectedTTT = formData.ContainsKey("TTT") ? (string)formData["TTT"] : "";
                ViewBag.SelectedCalismaGunuTipi = formData.ContainsKey("calismaGunuTipi") ? (int)formData["calismaGunuTipi"] : 1;
            }
            else
            {
                ViewBag.SelectedCalismaGunuTipi = 1;
            }

            return View();
        }

        private TimeSpan MaintenanceTime(DateTime productionStart, DateTime productionEnd)
        {
            loginkontrol();
            var overlappingMaintenance = db.Bakim
                .Where(m =>
                    m.Baslangic_Tarihi.HasValue && m.Bitis_Tarihi.HasValue &&
                    m.Baslangic_Tarihi <= productionEnd &&
                    m.Bitis_Tarihi >= productionStart)
                .Select(m => new
                {
                    Start = m.Baslangic_Tarihi > productionStart ? m.Baslangic_Tarihi.Value : productionStart,
                    End = m.Bitis_Tarihi < productionEnd ? m.Bitis_Tarihi.Value : productionEnd
                })
                .OrderBy(m => m.Start)
                .ToList();

            var mergedIntervals = new List<(DateTime Start, DateTime End)>();

            foreach (var interval in overlappingMaintenance)
            {
                if (mergedIntervals.Count == 0)
                {
                    mergedIntervals.Add((interval.Start, interval.End));
                }
                else
                {
                    var last = mergedIntervals.Last();
                    if (interval.Start <= last.End)
                    {
                        mergedIntervals[mergedIntervals.Count - 1] = (last.Start,
                            interval.End > last.End ? interval.End : last.End);
                    }
                    else
                    {
                        mergedIntervals.Add((interval.Start, interval.End));
                    }
                }
            }

            TimeSpan totalDowntime = TimeSpan.Zero;
            foreach (var interval in mergedIntervals)
            {
                totalDowntime += interval.End - interval.Start;
            }

            return totalDowntime;
        }

        private DateTime CalculateFinalDeliveryTime(DateTime productionStartDate, int totalUnitsToProduce, double productionRatePerHour, int overtimeHoursPerDay, int workingDaysType)
        {
            double dailyStandardWorkingHours = 8.0;
            double totalWorkingHoursPerDayCycle = dailyStandardWorkingHours + overtimeHoursPerDay;
            double remainingProductionHours = (double)totalUnitsToProduce / productionRatePerHour;
            DateTime currentCalculationTime = productionStartDate;

            int workingDayStartHour = 8;
            if (overtimeHoursPerDay > 6)
            {
                if (overtimeHoursPerDay == 15)
                {
                    workingDayStartHour = 0;
                }
                else
                {
                    workingDayStartHour -= (overtimeHoursPerDay - 6);
                }
            }

            while (remainingProductionHours > 0)
            {
                DateTime todayWorkingStartTime = currentCalculationTime.Date.AddHours(workingDayStartHour);
                DateTime todayWorkingEndTime = currentCalculationTime.Date.AddHours(workingDayStartHour + totalWorkingHoursPerDayCycle);

                bool isSaturday = currentCalculationTime.DayOfWeek == DayOfWeek.Saturday;
                bool isSunday = currentCalculationTime.DayOfWeek == DayOfWeek.Sunday;
                bool shouldSkipDay = false;

                if (workingDaysType == 0)
                {
                    if (isSaturday || isSunday) shouldSkipDay = true;
                }
                else if (workingDaysType == 1)
                {
                    if (isSunday) shouldSkipDay = true;
                }

                if (shouldSkipDay || currentCalculationTime >= todayWorkingEndTime)
                {
                    currentCalculationTime = currentCalculationTime.AddDays(1).Date.AddHours(workingDayStartHour);
                    continue;
                }

                if (currentCalculationTime < todayWorkingStartTime)
                {
                    currentCalculationTime = todayWorkingStartTime;
                }

                TimeSpan potentialProductionTimeInSegment = todayWorkingEndTime - currentCalculationTime;

                if (potentialProductionTimeInSegment.TotalHours <= 0)
                {
                    currentCalculationTime = currentCalculationTime.AddDays(1).Date.AddHours(workingDayStartHour);
                    continue;
                }

                TimeSpan maintenanceDowntimeInSegment = MaintenanceTime(currentCalculationTime, todayWorkingEndTime);

                double effectiveProductionHoursInSegment = potentialProductionTimeInSegment.TotalHours - maintenanceDowntimeInSegment.TotalHours;

                if (effectiveProductionHoursInSegment <= 0)
                {
                    currentCalculationTime = currentCalculationTime.AddDays(1).Date.AddHours(workingDayStartHour);
                    continue;
                }

                if (remainingProductionHours <= effectiveProductionHoursInSegment)
                {
                    currentCalculationTime = currentCalculationTime.AddHours(remainingProductionHours);
                    remainingProductionHours = 0;
                }
                else
                {
                    currentCalculationTime = currentCalculationTime.AddHours(effectiveProductionHoursInSegment);
                    remainingProductionHours -= effectiveProductionHoursInSegment;

                    currentCalculationTime = currentCalculationTime.Date.AddDays(1).AddHours(workingDayStartHour);
                }
            }
            return currentCalculationTime;
        }

        // POST: Order/Create
        [HttpPost]
        public ActionResult Create_SI(FormCollection collection)
        {
            loginkontrol();
            int SecilenUrunId = Convert.ToInt32(collection.Get("urun"));
            int SecilenMusteriId = Convert.ToInt32(collection.Get("musteri"));
            int SecilenMiktarDegeri = Convert.ToInt32(collection.Get("miktar"));
            int MesaiSaati = Convert.ToInt32(collection.Get("mesai"));
            DateTime TahminiTeslimTarihi = Convert.ToDateTime(collection.Get("TTT"));

            int workingDaysType = Convert.ToInt32(collection.Get("calismaGunuTipi"));

            bool? PazarDBValue = null;
            if (workingDaysType == 2)
            {
                PazarDBValue = true;
            }
            else
            {
                PazarDBValue = false;
            }

            var urun = db.Urun.FirstOrDefault(c => c.Id == SecilenUrunId);
            if (urun == null)
            {
                TempData["ErrorMessage"] = "Seçilen ürün bulunamadı.";
                TempData["FormData"] = new Dictionary<string, object>
                {
                    { "musteri", SecilenMusteriId },
                    { "urun", SecilenUrunId },
                    { "miktar", SecilenMiktarDegeri },
                    { "mesai", MesaiSaati },
                    { "TTT", TahminiTeslimTarihi.ToString("yyyy-MM-ddTHH:mm") },
                    { "calismaGunuTipi", workingDaysType }
                };
                return RedirectToAction("Create_SI");
            }

            var receteler = db.Recete.Where(r => r.Urun_FK == urun.Id).ToList();

            bool stokYetersiz = false;
            string eksikHammaddeler = "";

            foreach (var recete in receteler)
            {
                int gerekenMiktar = recete.Miktar.Value * SecilenMiktarDegeri;

                var hammadde = db.Ham_Madde.FirstOrDefault(h => h.Id == recete.Ham_Madde_FK);
                if (hammadde == null)
                {
                    TempData["ErrorMessage"] = "Reçetede tanımlı hammadde bulunamadı.";
                    TempData["FormData"] = new Dictionary<string, object>
                    {
                        { "musteri", SecilenMusteriId },
                        { "urun", SecilenUrunId },
                        { "miktar", SecilenMiktarDegeri },
                        { "mesai", MesaiSaati },
                        { "TTT", TahminiTeslimTarihi.ToString("yyyy-MM-ddTHH:mm") },
                        { "calismaGunuTipi", workingDaysType }
                    };
                    return RedirectToAction("Create_SI");
                }

                var stok = db.Stok.FirstOrDefault(s => s.Ham_Madde_FK == hammadde.Id);

                if (stok == null || stok.Miktar < gerekenMiktar)
                {
                    stokYetersiz = true;
                    eksikHammaddeler += hammadde.Ham_Madde1 + ": ";

                    string gerekenMiktarStr = gerekenMiktar.ToString("N0");
                    string stokMiktarStr = (stok?.Miktar ?? 0).ToString("N0");
                    string eksikMiktarStr = (gerekenMiktar - (stok?.Miktar ?? 0)).ToString("N0");

                    string birimName = hammadde.Birim?.Birim1 ?? "Adet";

                    if (birimName == "Ağırlık")
                    {
                        if ((gerekenMiktar - (stok?.Miktar ?? 0)) > 1000)
                        {
                            int eksikKG = (gerekenMiktar - (stok?.Miktar ?? 0)) / 1000;
                            int eksikGR = (gerekenMiktar - (stok?.Miktar ?? 0)) % 1000;
                            eksikHammaddeler += $"Gereken: {gerekenMiktarStr} GR, Stok: {stokMiktarStr} GR, Eksik: {eksikKG} KG {eksikGR} GR<br>";
                        }
                        else
                        {
                            eksikHammaddeler += $"Gereken: {gerekenMiktarStr} GR, Stok: {stokMiktarStr} GR, Eksik: {eksikMiktarStr} GR<br>";
                        }
                    }
                    else if (birimName == "Uzunluk")
                    {
                        if ((gerekenMiktar - (stok?.Miktar ?? 0)) > 1000)
                        {
                            int eksikMetre = (gerekenMiktar - (stok?.Miktar ?? 0)) / 1000;
                            int eksikCM = (gerekenMiktar - (stok?.Miktar ?? 0)) % 1000;
                            eksikHammaddeler += $"Gereken: {gerekenMiktarStr} CM, Stok: {stokMiktarStr} CM, Eksik: {eksikMetre} Metre {eksikCM} CM<br>";
                        }
                        else
                        {
                            eksikHammaddeler += $"Gereken: {gerekenMiktarStr} CM, Stok: {stokMiktarStr} CM, Eksik: {eksikMiktarStr} CM<br>";
                        }
                    }
                    else if (birimName == "Hacim")
                    {
                        if ((gerekenMiktar - (stok?.Miktar ?? 0)) > 1000)
                        {
                            int eksikLitre = (gerekenMiktar - (stok?.Miktar ?? 0)) / 1000;
                            int eksikML = (gerekenMiktar - (stok?.Miktar ?? 0)) % 1000;
                            eksikHammaddeler += $"Gereken: {gerekenMiktarStr} ML, Stok: {stokMiktarStr} ML, Eksik: {eksikLitre} Litre {eksikML} ML<br>";
                        }
                        else
                        {
                            eksikHammaddeler += $"Gereken: {gerekenMiktarStr} ML, Stok: {stokMiktarStr} ML, Eksik: {eksikMiktarStr} ML<br>";
                        }
                    }
                    else
                    {
                        eksikHammaddeler += $"Gereken: {gerekenMiktarStr} Adet, Stok: {stokMiktarStr} Adet, Eksik: {eksikMiktarStr} Adet<br>";
                    }
                }
            }

            if (stokYetersiz)
            {
                TempData["ErrorMessage"] = "Bu ürünün üretimi için aşağıdaki hammaddelerden stoğa gerekli miktar eklenmelidir:<br><br>" + eksikHammaddeler;
                TempData["FormData"] = new Dictionary<string, object>
                {
                    { "musteri", SecilenMusteriId },
                    { "urun", SecilenUrunId },
                    { "miktar", SecilenMiktarDegeri },
                    { "mesai", MesaiSaati },
                    { "TTT", TahminiTeslimTarihi.ToString("yyyy-MM-ddTHH:mm") },
                    { "calismaGunuTipi", workingDaysType }
                };
                return RedirectToAction("Create_SI");
            }

            double productionRate = 0;
            if (urun.Fabrika_Kapasitesi.HasValue && urun.Fabrika_Kapasitesi.Value > 0)
            {
                productionRate = (double)urun.Fabrika_Kapasitesi.Value;
            }
            else
            {
                TempData["ErrorMessage"] = "Ürünün fabrika kapasitesi tanımlı değil veya sıfır. Lütfen ürün bilgilerini güncelleyiniz.";
                TempData["FormData"] = new Dictionary<string, object>
                {
                    { "musteri", SecilenMusteriId },
                    { "urun", SecilenUrunId },
                    { "miktar", SecilenMiktarDegeri },
                    { "mesai", MesaiSaati },
                    { "TTT", TahminiTeslimTarihi.ToString("yyyy-MM-ddTHH:mm") },
                    { "calismaGunuTipi", workingDaysType }
                };
                return RedirectToAction("Create_SI");
            }

            DateTime finalCalculatedDeliveryTime = CalculateFinalDeliveryTime(
                DateTime.Now,
                SecilenMiktarDegeri,
                productionRate,
                MesaiSaati,
                workingDaysType
            );

            if (TahminiTeslimTarihi < finalCalculatedDeliveryTime)
            {
                TempData["ErrorMessage"] = $"Seçilen teslim tarihi çok yakın. En erken teslim tarihi: {finalCalculatedDeliveryTime.ToString("yyyy-MM-dd HH:mm")}. Lütfen daha ileri bir tarih seçiniz.";
                TempData["FormData"] = new Dictionary<string, object>
                {
                    { "musteri", SecilenMusteriId },
                    { "urun", SecilenUrunId },
                    { "miktar", SecilenMiktarDegeri },
                    { "mesai", MesaiSaati },
                    { "TTT", TahminiTeslimTarihi.ToString("yyyy-MM-ddTHH:mm") },
                    { "calismaGunuTipi", workingDaysType }
                };
                return RedirectToAction("Create_SI");
            }

            foreach (var recete in receteler)
            {
                int gerekenMiktar = recete.Miktar.Value * SecilenMiktarDegeri;
                var stok = db.Stok.FirstOrDefault(s => s.Ham_Madde_FK == recete.Ham_Madde_FK);
                if (stok != null)
                {
                    stok.Miktar -= gerekenMiktar;
                }
                else
                {
                    db.Stok.Add(new Stok { Ham_Madde_FK = recete.Ham_Madde_FK, Miktar = -gerekenMiktar });
                }
            }

            Siparis spr = new Siparis()
            {
                Urun_FK = SecilenUrunId,
                Miktar = SecilenMiktarDegeri,
                Musteri_FK = SecilenMusteriId,
                Teslim_Tarihi = TahminiTeslimTarihi,
                Siparis_Tarihi = DateTime.Now,
                Silindi = false,
                Mesai = MesaiSaati,
                Pazar = PazarDBValue
            };

            db.Siparis.Add(spr);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Sipariş başarıyla oluşturuldu.";
            return RedirectToAction("Index_SI");
        }

        // GET: Order/Edit/5
        public ActionResult Edit_SI(int id)
        {
            loginkontrol();
            var order = db.Siparis
                .Include(s => s.Musteri)
                .Include(s => s.Urun)
                .FirstOrDefault(s => s.Id == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            var musteriler = db.Musteri.Where(c => c.Silindi == false).OrderByDescending(m => m.Id).ToList();
            var urunler = db.Urun.Where(c => c.Silindi == false).OrderByDescending(u => u.Id).ToList();

            var productDisplayNames = new Dictionary<int, string>();
            foreach (var urun in urunler)
            {
                var receteler = db.Recete.Where(r => r.Urun_FK == urun.Id).ToList();
                var hammaddeler = receteler.Select(recete =>
                {
                    var hammadde = db.Ham_Madde.FirstOrDefault(h => h.Id == recete.Ham_Madde_FK);
                    return $"{recete.Miktar} {hammadde?.Ham_Madde1}";
                }).ToList();
                productDisplayNames[urun.Id] = $"{urun.Urun_Ad} ({string.Join(", ", hammaddeler)})";
            }

            ViewBag.customers = musteriler;
            ViewBag.products = urunler;
            ViewBag.productDisplayNames = productDisplayNames;

            int selectedWorkingDaysType;
            if (TempData["FormData"] is Dictionary<string, object> formData)
            {
                ViewBag.SelectedCustomerId = formData.ContainsKey("musteri") ? (int)formData["musteri"] : order.Musteri_FK;
                ViewBag.SelectedUrunId = formData.ContainsKey("urun") ? (int)formData["urun"] : order.Urun_FK;
                ViewBag.SelectedMiktar = formData.ContainsKey("miktar") ? (int)formData["miktar"] : order.Miktar;
                ViewBag.SelectedMesai = formData.ContainsKey("mesai") ? (int)formData["mesai"] : order.Mesai;
                ViewBag.SelectedTTT = formData.ContainsKey("TTT") ? (string)formData["TTT"] : order.Teslim_Tarihi?.ToString("yyyy-MM-ddTHH:mm");
                selectedWorkingDaysType = formData.ContainsKey("calismaGunuTipi") ? (int)formData["calismaGunuTipi"] : (order.Pazar.HasValue ? (order.Pazar.Value ? 2 : 1) : 1);
            }
            else
            {
                ViewBag.SelectedCustomerId = order.Musteri_FK;
                ViewBag.SelectedUrunId = order.Urun_FK;
                ViewBag.SelectedMiktar = order.Miktar;
                ViewBag.SelectedMesai = order.Mesai;
                ViewBag.SelectedTTT = order.Teslim_Tarihi?.ToString("yyyy-MM-ddTHH:mm");
                selectedWorkingDaysType = order.Pazar.HasValue ? (order.Pazar.Value ? 2 : 1) : 1;
            }
            ViewBag.SelectedCalismaGunuTipi = selectedWorkingDaysType;

            return View(order);
        }

        // POST: Order/Edit/5
        [HttpPost]
        public ActionResult Edit_SI(FormCollection collection)
        {
            loginkontrol();
            int orderId = Convert.ToInt32(collection.Get("Id"));
            var order = db.Siparis.Find(orderId);

            if (order == null)
            {
                return HttpNotFound();
            }

            int originalUrunId = order.Urun_FK.Value;
            int originalMiktar = order.Miktar.Value;

            int SecilenUrunId = Convert.ToInt32(collection.Get("urun"));
            int SecilenMusteriId = Convert.ToInt32(collection.Get("musteri"));
            int SecilenMiktarDegeri = Convert.ToInt32(collection.Get("miktar"));
            int MesaiSaati = Convert.ToInt32(collection.Get("mesai"));
            DateTime TahminiTeslimTarihi = Convert.ToDateTime(collection.Get("TTT"));

            int workingDaysType = Convert.ToInt32(collection.Get("calismaGunuTipi"));

            bool? PazarDBValue = null;
            if (workingDaysType == 2)
            {
                PazarDBValue = true;
            }
            else
            {
                PazarDBValue = false;
            }

            var urun = db.Urun.FirstOrDefault(c => c.Id == SecilenUrunId);
            if (urun == null)
            {
                TempData["ErrorMessage"] = "Seçilen ürün bulunamadı.";
                TempData["FormData"] = new Dictionary<string, object>
                {
                    { "musteri", SecilenMusteriId },
                    { "urun", SecilenUrunId },
                    { "miktar", SecilenMiktarDegeri },
                    { "mesai", MesaiSaati },
                    { "TTT", TahminiTeslimTarihi.ToString("yyyy-MM-ddTHH:mm") },
                    { "calismaGunuTipi", workingDaysType }
                };
                return RedirectToAction("Edit_SI", new { id = orderId });
            }
            var receteler = db.Recete.Where(r => r.Urun_FK == urun.Id).ToList();

            bool stokYetersiz = false;
            string eksikHammaddeler = "";

            var stockChanges = new Dictionary<int, int>();

            if (originalUrunId != SecilenUrunId || originalMiktar != SecilenMiktarDegeri)
            {
                var originalReceteler = db.Recete.Where(r => r.Urun_FK == originalUrunId).ToList();
                foreach (var recete in originalReceteler)
                {
                    int hammaddeId = Convert.ToInt32(recete.Ham_Madde_FK);
                    int releasedMiktar = recete.Miktar.Value * originalMiktar;

                    int currentChange;
                    if (stockChanges.TryGetValue(hammaddeId, out currentChange))
                    {
                        stockChanges[hammaddeId] = currentChange + releasedMiktar;
                    }
                    else
                    {
                        stockChanges.Add(hammaddeId, releasedMiktar);
                    }
                }
            }

            foreach (var recete in receteler)
            {
                int gerekenMiktar = recete.Miktar.Value * SecilenMiktarDegeri;
                int hammaddeId = Convert.ToInt32(recete.Ham_Madde_FK);

                var currentStokInDb = db.Stok.FirstOrDefault(s => s.Ham_Madde_FK == hammaddeId);

                int pendingStockChange = 0;
                stockChanges.TryGetValue(hammaddeId, out pendingStockChange);

                int simulatedCurrentStock = (currentStokInDb?.Miktar ?? 0) + pendingStockChange;

                if (simulatedCurrentStock < gerekenMiktar)
                {
                    stokYetersiz = true;
                    var hammadde = db.Ham_Madde.FirstOrDefault(h => h.Id == hammaddeId);

                    if (hammadde == null)
                    {
                        eksikHammaddeler += $"Bilinmeyen Hammadde (ID: {hammaddeId}): ";
                    }
                    else
                    {
                        eksikHammaddeler += hammadde.Ham_Madde1 + ": ";
                    }

                    string gerekenMiktarStr = gerekenMiktar.ToString("N0");
                    string stokMiktarStr = simulatedCurrentStock.ToString("N0");
                    string eksikMiktarStr = (gerekenMiktar - simulatedCurrentStock).ToString("N0");

                    string birimName = hammadde?.Birim?.Birim1 ?? "Adet";

                    if (birimName == "Ağırlık")
                    {
                        if ((gerekenMiktar - simulatedCurrentStock) >= 1000)
                        {
                            int eksikKG = (gerekenMiktar - simulatedCurrentStock) / 1000;
                            int eksikGR = (gerekenMiktar - simulatedCurrentStock) % 1000;
                            eksikHammaddeler += $"Gereken: {gerekenMiktarStr} GR, Stok: {stokMiktarStr} GR, Eksik: {eksikKG} KG {eksikGR} GR<br>";
                        }
                        else
                        {
                            eksikHammaddeler += $"Gereken: {gerekenMiktarStr} GR, Stok: {stokMiktarStr} GR, Eksik: {eksikMiktarStr} GR<br>";
                        }
                    }
                    else if (birimName == "Uzunluk")
                    {
                        if ((gerekenMiktar - simulatedCurrentStock) >= 1000)
                        {
                            int eksikMetre = (gerekenMiktar - simulatedCurrentStock) / 1000;
                            int eksikCM = (gerekenMiktar - simulatedCurrentStock) % 1000;
                            eksikHammaddeler += $"Gereken: {gerekenMiktarStr} CM, Stok: {stokMiktarStr} CM, Eksik: {eksikMetre} Metre {eksikCM} CM<br>";
                        }
                        else
                        {
                            eksikHammaddeler += $"Gereken: {gerekenMiktarStr} CM, Stok: {stokMiktarStr} CM, Eksik: {eksikMiktarStr} CM<br>";
                        }
                    }
                    else if (birimName == "Hacim")
                    {
                        if ((gerekenMiktar - simulatedCurrentStock) >= 1000)
                        {
                            int eksikLitre = (gerekenMiktar - simulatedCurrentStock) / 1000;
                            int eksikML = (gerekenMiktar - simulatedCurrentStock) % 1000;
                            eksikHammaddeler += $"Gereken: {gerekenMiktarStr} ML, Stok: {stokMiktarStr} ML, Eksik: {eksikLitre} Litre {eksikML} ML<br>";
                        }
                        else
                        {
                            eksikHammaddeler += $"Gereken: {gerekenMiktarStr} ML, Stok: {stokMiktarStr} ML, Eksik: {eksikMiktarStr} ML<br>";
                        }
                    }
                    else
                    {
                        eksikHammaddeler += $"Gereken: {gerekenMiktarStr} Adet, Stok: {stokMiktarStr} Adet, Eksik: {eksikMiktarStr} Adet<br>";
                    }
                }
                else
                {
                    int currentChange;
                    if (stockChanges.TryGetValue(hammaddeId, out currentChange))
                    {
                        stockChanges[hammaddeId] = currentChange - gerekenMiktar;
                    }
                    else
                    {
                        stockChanges.Add(hammaddeId, -gerekenMiktar);
                    }
                }
            }

            if (stokYetersiz)
            {
                TempData["ErrorMessage"] = "Bu ürünün üretimi için aşağıdaki hammaddelerden stoğa gerekli miktar eklenmelidir:<br><br>" + eksikHammaddeler;
                TempData["FormData"] = new Dictionary<string, object>
                {
                    { "musteri", SecilenMusteriId },
                    { "urun", SecilenUrunId },
                    { "miktar", SecilenMiktarDegeri },
                    { "mesai", MesaiSaati },
                    { "TTT", TahminiTeslimTarihi.ToString("yyyy-MM-ddTHH:mm") },
                    { "calismaGunuTipi", workingDaysType }
                };
                return RedirectToAction("Edit_SI", new { id = orderId });
            }

            double productionRate = 0;
            if (urun.Fabrika_Kapasitesi.HasValue && urun.Fabrika_Kapasitesi.Value > 0)
            {
                productionRate = (double)urun.Fabrika_Kapasitesi.Value;
            }
            else
            {
                TempData["ErrorMessage"] = "Ürünün fabrika kapasitesi tanımlı değil veya sıfır. Lütfen ürün bilgilerini güncelleyiniz.";
                TempData["FormData"] = new Dictionary<string, object>
                {
                    { "musteri", SecilenMusteriId },
                    { "urun", SecilenUrunId },
                    { "miktar", SecilenMiktarDegeri },
                    { "mesai", MesaiSaati },
                    { "TTT", TahminiTeslimTarihi.ToString("yyyy-MM-ddTHH:mm") },
                    { "calismaGunuTipi", workingDaysType }
                };
                return RedirectToAction("Edit_SI", new { id = orderId });
            }

            DateTime finalCalculatedDeliveryTime = CalculateFinalDeliveryTime(
                DateTime.Now,
                SecilenMiktarDegeri,
                productionRate,
                MesaiSaati,
                workingDaysType
            );

            if (TahminiTeslimTarihi < finalCalculatedDeliveryTime)
            {
                TempData["ErrorMessage"] = $"Seçilen teslim tarihi çok yakın. En erken teslim tarihi: {finalCalculatedDeliveryTime.ToString("yyyy-MM-dd HH:mm")}. Lütfen daha ileri bir tarih seçiniz.";
                TempData["FormData"] = new Dictionary<string, object>
                {
                    { "musteri", SecilenMusteriId },
                    { "urun", SecilenUrunId },
                    { "miktar", SecilenMiktarDegeri },
                    { "mesai", MesaiSaati },
                    { "TTT", TahminiTeslimTarihi.ToString("yyyy-MM-ddTHH:mm") },
                    { "calismaGunuTipi", workingDaysType }
                };
                return RedirectToAction("Edit_SI", new { id = orderId });
            }

            foreach (var change in stockChanges)
            {
                var stok = db.Stok.FirstOrDefault(s => s.Ham_Madde_FK == change.Key);
                if (stok != null)
                {
                    stok.Miktar += change.Value;
                }
                else if (change.Value != 0)
                {
                    db.Stok.Add(new Stok { Ham_Madde_FK = change.Key, Miktar = change.Value });
                }
            }

            order.Urun_FK = SecilenUrunId;
            order.Musteri_FK = SecilenMusteriId;
            order.Miktar = SecilenMiktarDegeri;
            order.Teslim_Tarihi = TahminiTeslimTarihi;
            order.Mesai = MesaiSaati;
            order.Pazar = PazarDBValue;

            db.SaveChanges();
            TempData["SuccessMessage"] = "Sipariş başarıyla güncellendi.";
            return RedirectToAction("Index_SI");
        }

        // GET: Order/Delete/5
        public ActionResult Delete_SI(int id)
        {
            var siparis = db.Siparis.FirstOrDefault(s => s.Id == id);
            if (siparis != null && siparis.Silindi == false)
            {
                int urunId = siparis.Urun_FK.Value;
                int siparisMiktari = siparis.Miktar.Value;

                var receteler = db.Recete.Where(r => r.Urun_FK == urunId).ToList();

                foreach (var recete in receteler)
                {
                    int hammaddeId = recete.Ham_Madde_FK.Value;
                    int kullanilanMiktar = recete.Miktar.Value * siparisMiktari;

                    var stok = db.Stok.FirstOrDefault(s => s.Ham_Madde_FK == hammaddeId);
                    if (stok != null)
                    {
                        stok.Miktar += kullanilanMiktar;
                    }
                    else
                    {
                        db.Stok.Add(new Stok
                        {
                            Ham_Madde_FK = hammaddeId,
                            Miktar = kullanilanMiktar
                        });
                    }
                }

                siparis.Silindi = true;

                db.SaveChanges();
                TempData["SuccessMessage"] = "Sipariş başarıyla silindi ve hammaddeler stoğa geri eklendi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Sipariş bulunamadı veya zaten silinmişti.";
            }

            return RedirectToAction("Index_SI");
        }

        // POST: Order/Delete/5
        [HttpPost]
        public ActionResult Delete_SI(int id, FormCollection collection)
        {
            try
            {
                return Delete_SI(id);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Sipariş silinirken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index_SI");
            }
        }
    }
}