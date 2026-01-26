using MDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MDK.Controllers
{
    public class SettingsController : Controller
    {
        private Entities db = new Entities();
        // GET: Settings
        public void loginkontrol()
        {
            if (Session["email"] == null)
            {
                Response.Redirect("/Auth/Login");
            }
        }
        public ActionResult Index_Se()
        {
            loginkontrol();
            List<Ayarlar> ayar = db.Ayarlar.ToList();
            return View(ayar);
        }
        // GET: Setting/Edit/5
        public ActionResult Edit_Se(int id)
        {
            loginkontrol();
            var ayar = db.Ayarlar.FirstOrDefault(c => c.Id == id);
            return View(ayar);
        }

        // POST: Setting/Edit/5
        [HttpPost]
        public ActionResult Edit_Se(int id, FormCollection collection)
        {
            loginkontrol();
            string deger = collection.Get("ayardegeri");

            Ayarlar ayar = db.Ayarlar.FirstOrDefault(a => a.Id == id);
            if (ayar != null)
            {
                ayar.Deger = deger;
                db.SaveChanges();
            }
            return RedirectToAction("Index_Se");
        }
    }
}