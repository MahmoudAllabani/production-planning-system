using MDK.Models;
using Rotativa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MDK.Controllers
{
    public class EmployeeController : Controller
    {
        private Entities db = new Entities();
        public void loginkontrol()
        {
            if (Session["email"] == null)
            {
                Response.Redirect("/Auth/Login");
            }
        }

        // GET: Employee
        public ActionResult Index_EM()
        {
            loginkontrol();
            List<Personel> employee = db.Personel.Where(c => c.Silindi == false).OrderBy(c => c.Ad).ToList();
            return View("Index_EM", employee);
        }

        // Export to PDF
        public ActionResult ExportPdf_EM()
        {
            var personel = db.Personel
                             .Where(c => c.Silindi == false)
                             .OrderBy(c => c.Id)
                             .ToList();

            return new ViewAsPdf("Index_EM", personel)
            {
                FileName = "PersonelListele.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Landscape,
                PageMargins = new Rotativa.Options.Margins { Top = 10, Bottom = 10, Left = 10, Right = 10 },
                CustomSwitches = "--user-style-sheet " + Server.MapPath("~/purple/excel-print.css")
            };
        }

        // GET: Employee/Details/5
        public ActionResult Details_EM(int id)
        {
            loginkontrol();
            var employee = db.Personel.FirstOrDefault(c => c.Id == id);
            if (employee == null)
            {
                return HttpNotFound();  // Return 404 if employee not found
            }
            return View("Details_EM", employee);
        }

        // GET: Employee/Create
        public ActionResult Create_EM()
        {
            loginkontrol();
            var model = new Personel
            {
                Gmail = new Gmail(),
                Adres = new Adres(),
                Telefon = new Telefon(),
                Tc_Bilgileri = new Tc_Bilgileri()
            };
            return View("Create_EM", model);
        }

        // POST: Employee/Create
        [HttpPost]
        public ActionResult Create_EM(Personel model)
        {
            loginkontrol();
            try
            {
                if (ModelState.IsValid)
                {
                    // Create related entities
                    var address = new Adres { Adres_Bilgisi = model.Adres.Adres_Bilgisi };
                    var phone = new Telefon { Telefon_No = model.Telefon.Telefon_No };
                    var email = new Gmail { Gmail1 = model.Gmail.Gmail1 };
                    var identity = new Tc_Bilgileri { Tc_No = model.Tc_Bilgileri.Tc_No };

                    // Assign properties to the new Personel object
                    var newPersonel = new Personel
                    {
                        Ad = model.Ad,
                        Soyad = model.Soyad,
                        Yas = model.Yas,
                        Maas = model.Maas,
                        Medeni_Durumu = model.Medeni_Durumu,
                        Cocuk_Sayisi = model.Cocuk_Sayisi,
                        Cinsiyet = model.Cinsiyet,
                        Silindi = false,
                        Adres = address,
                        Telefon = phone,
                        Gmail = email,
                        Tc_Bilgileri = identity
                    };

                    // Add to the database
                    db.Personel.Add(newPersonel);
                    db.SaveChanges();

                    return RedirectToAction("Index_EM");
                }
                return View("Create_EM", model);
            }
            catch (Exception ex)
            {
                // Log the error
                ViewBag.ErrorMessage = "An error occurred: " + ex.Message;
                return View("Create_EM", model);
            }
        }

        // GET: Employee/Edit/5
        public ActionResult Edit_EM(int id)
        {
            loginkontrol();
            var employee = db.Personel
                .Include("Adres")
                .Include("Gmail")
                .Include("Tc_Bilgileri")
                .Include("Telefon")
                .FirstOrDefault(c => c.Id == id && c.Silindi == false);

            if (employee == null)
            {
                return HttpNotFound();
            }

            return View("Edit_EM", employee);
        }

        // POST: Employee/Edit/5
        [HttpPost]
        public ActionResult Edit_EM(int id, FormCollection collection)
        {
            loginkontrol();
            try
            {
                var employee = db.Personel
                    .Include("Adres")
                    .Include("Gmail")
                    .Include("Tc_Bilgileri")
                    .Include("Telefon")
                    .FirstOrDefault(c => c.Id == id && c.Silindi == false);

                if (employee == null)
                {
                    return HttpNotFound();
                }

                // Update basic employee information
                employee.Ad = collection.Get("employeeName");
                employee.Soyad = collection.Get("employeeSurname");
                employee.Yas = Convert.ToInt32(collection.Get("employeeYas"));
                employee.Maas = Convert.ToInt32(collection.Get("employeeMaas"));
                employee.Medeni_Durumu = collection.Get("employeeMedeniHal");
                employee.Cocuk_Sayisi = Convert.ToInt32(collection.Get("employeeCocuksayisi"));
                employee.Cinsiyet = collection.Get("employeeCinsiyet") == "kadin" ? true :
                                  (collection.Get("employeeCinsiyet") == "erkek" ? false : (bool?)null);

                // Update related entities
                employee.Adres.Adres_Bilgisi = collection.Get("employeeEvadresi");
                employee.Telefon.Telefon_No = collection.Get("employeeTelefon");
                employee.Gmail.Gmail1 = collection.Get("employeeEmail");
                employee.Tc_Bilgileri.Tc_No = collection.Get("employeeKimlikno");

                db.SaveChanges();

                return RedirectToAction("Index_EM"); // Updated redirect
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred: " + ex.Message;
                return View("Edit_EM");
            }
        }

        // GET: Employee/Delete/5
        public ActionResult Delete_EM(int id)
        {
            var employee = db.Personel.FirstOrDefault(m => m.Id == id);
            if (employee != null)
            {
                employee.Silindi = true;
                db.SaveChanges();
            }

            return RedirectToAction("Index_EM"); // Updated redirect
        }
    }
}