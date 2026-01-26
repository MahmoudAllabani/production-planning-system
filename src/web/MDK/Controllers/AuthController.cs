using MDK.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using BCryptNet = BCrypt.Net.BCrypt;

namespace MDK.Controllers
{
    public class AuthController : Controller
    {
        Entities db = new Entities();
        // GET: Auth
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Login()
        {
            return View("login");
        }
        [HttpPost]
        public ActionResult Login(FormCollection collection)
        {
            // Formdan gelen değerler (eksik olan buydu)
            string email = collection.Get("email");
            string password = collection.Get("password");

            var emailRow = db.Ayarlar.FirstOrDefault(u => u.Ad == "yonetici_email");
            var passRow = db.Ayarlar.FirstOrDefault(u => u.Ad == "yonetici_sifre");

            if (emailRow == null || passRow == null)
                return View("loginerror");

            string emailAyar = emailRow.Deger?.ToString();
            string passwordAyar = passRow.Deger?.ToString();

            if (string.IsNullOrWhiteSpace(emailAyar) || string.IsNullOrWhiteSpace(passwordAyar))
                return View("loginerror");

            // Ek güvenlik: form boş geldiyse
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return View("loginerror");

            bool isPasswordValid = BCryptNet.Verify(password, passwordAyar);

            if (isPasswordValid && emailAyar == email)
            {
                Session["email"] = email;
                return Redirect("/");
            }

            return View("loginerror");
        }

        public ActionResult Newpassword()
        {
            return View("newpassword");
        }
        [HttpPost]
        public ActionResult Newpassword(FormCollection collection)
        {
            string password = collection.Get("password");

            string hashedPassword = BCryptNet.HashPassword(password);

            db.Ayarlar.FirstOrDefault(u => u.Ad == "yonetici_sifre").Deger = hashedPassword;
            db.SaveChanges();

            return Redirect("/");

        }
        public ActionResult Logout()
        {
            Session.Abandon();
            return Redirect("/Auth/Login");
        }
        public ActionResult LoginError()
        {
            return View("LoginError");
        }
    }
}