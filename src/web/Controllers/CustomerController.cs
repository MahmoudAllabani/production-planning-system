using MDK.Models;
using Rotativa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MDK.Controllers
{
    public class CustomerController : Controller
    {
        private Entities db = new Entities();
        public void loginkontrol()
        {
            if (Session["email"] == null)
            {
                Response.Redirect("/Auth/Login");
            }
        }

        public ActionResult Index_MU()
        {
            loginkontrol();
            List<Musteri> customers = db.Musteri.Where(c => c.Silindi == false).OrderBy(c => c.Ad).ToList();
            return View("Index_MU", customers);
        }

        // Export to PDF
        public ActionResult ExportPdf_MU()
        {
            var musteri = db.Musteri
                             .Where(c => c.Silindi == false)
                             .OrderBy(c => c.Id)
                             .ToList();

            return new ViewAsPdf("Index_MU", musteri)
            {
                FileName = "MusteriListele.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Landscape,
                PageMargins = new Rotativa.Options.Margins { Top = 10, Bottom = 10, Left = 10, Right = 10 },
                CustomSwitches = "--user-style-sheet " + Server.MapPath("~/purple/excel-print.css")
            };
        }

        // GET: Customer/Details/5
        public ActionResult Details_MU(int id)
        {
            loginkontrol();
            var customer = db.Musteri.FirstOrDefault(c => c.Id == id);
            if (customer == null)
            {
                return HttpNotFound();
            }
            return View("Details_MU", customer);
        }

        // GET: Customer/Create
        public ActionResult Create_MU()
        {
            loginkontrol();
            return View("Create_MU");
        }

        // POST: Customer/Create
        [HttpPost]
        public ActionResult Create_MU(FormCollection collection)
        {
            loginkontrol();
            string name = collection.Get("Ad");
            string surname = collection.Get("Soyad");
            string address = collection.Get("Adres");
            string emaili = collection.Get("Email");
            string identificationNumber = collection.Get("TC");
            string phone = collection.Get("Telefon");

            Adres adressdegiskeni = new Adres()
            {
                Adres_Bilgisi = address
            };

            // Telefon tablosuna kayıt ekleme
            Telefon tel = new Telefon()
            {
                Telefon_No = phone
            };

            Gmail mail = new Gmail()
            {
                Gmail1 = emaili
            };

            Tc_Bilgileri kimlik = new Tc_Bilgileri()
            {
                Tc_No = identificationNumber
            };

            Musteri mstr = new Musteri()
            {
                Ad = name,
                Soyad = surname,
                Adres = adressdegiskeni,
                Gmail = mail,
                Tc_Bilgileri = kimlik,
                Telefon = tel,
                Silindi = false
            };

            db.Musteri.Add(mstr);
            db.SaveChanges();

            return RedirectToAction("Index_MU");
        }

        // GET: Customer/Edit/5
        public ActionResult Edit_MU(int id)
        {
            loginkontrol();
            var customer = db.Musteri
                .Include("Adres")
                .Include("Gmail")
                .Include("Tc_Bilgileri")
                .Include("Telefon")
                .FirstOrDefault(c => c.Id == id);

            if (customer == null)
            {
                return HttpNotFound();
            }

            return View("Edit_MU", customer);
        }

         [HttpPost]
        public ActionResult Edit_MU(int id, FormCollection collection)
        {
            loginkontrol();
            try
            {
                var customer = db.Musteri
                    .Include("Adres")
                    .Include("Gmail")
                    .Include("Tc_Bilgileri")
                    .Include("Telefon")
                    .FirstOrDefault(c => c.Id == id);

                if (customer == null)
                {
                    return HttpNotFound();
                }

                customer.Ad = collection.Get("Ad");
                customer.Soyad = collection.Get("Soyad");
                customer.Adres.Adres_Bilgisi = collection.Get("Adres");
                customer.Gmail.Gmail1 = collection.Get("Email");
                customer.Tc_Bilgileri.Tc_No = collection.Get("TC");
                customer.Telefon.Telefon_No = collection.Get("Telefon");

                db.SaveChanges();

                return RedirectToAction("Index_MU");
            }
            catch
            {
                return View("Edit_MU");
            }
        }

        // GET: Customer/Delete/5
        public ActionResult Delete_MU(int id)
        {
            var musteri = db.Musteri.FirstOrDefault(m => m.Id == id);

            if (musteri != null)
            {
                musteri.Silindi = true;
                db.SaveChanges();
            }

            return RedirectToAction("Index_MU");
        }

        // POST: Customer/Delete/5
        [HttpPost]
        public ActionResult Delete_MU(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here
                return RedirectToAction("Index_MU");
            }
            catch
            {
                return View("Index_MU");
            }
        }
    }
}