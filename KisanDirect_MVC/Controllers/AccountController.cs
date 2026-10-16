using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using KisanDirect_MVC.Models;

namespace KisanDirect_MVC.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(
            string fullName, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                password.Length < 8)
            {
                ViewBag.Message =
                    "Fill all fields. Password must be at least 8 characters.";
                return View();
            }

            string cs = ConfigurationManager
                .ConnectionStrings["KisanDirectConnection"]
                .ConnectionString;

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    // Check whether email already exists
                    using (SqlCommand check = new SqlCommand(
                        "SELECT COUNT(*) FROM dbo.Users " +
                        "WHERE Email = @Email", con))
                    {
                        check.Parameters.Add("@Email", SqlDbType.VarChar, 150)
                            .Value = email.Trim();

                        int count = Convert.ToInt32(check.ExecuteScalar());

                        if (count > 0)
                        {
                            ViewBag.Message =
                                "This email is already registered. Please log in.";
                            ViewBag.Email = email.Trim();
                            return View();
                        }
                    }

                    string hash = PasswordHelper.HashPassword(password);

                    using (SqlCommand cmd = new SqlCommand(
                        "INSERT INTO dbo.Users " +
                        "(FullName, Email, PasswordHash, Role) " +
                        "VALUES (@Name, @Email, @Hash, 'Farmer')", con))
                    {
                        cmd.Parameters.Add("@Name", SqlDbType.VarChar, 100)
                            .Value = fullName.Trim();

                        cmd.Parameters.Add("@Email", SqlDbType.VarChar, 150)
                            .Value = email.Trim();

                        cmd.Parameters.Add("@Hash", SqlDbType.VarChar, 255)
                            .Value = hash;

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["AccountMessage"] =
                    "Registration successful. Please log in.";

                return RedirectToAction("Login");
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                {
                    ViewBag.Message =
                        "This email is already registered. Please log in.";
                    ViewBag.Email = email.Trim();
                }
                else
                {
                    ViewBag.Message =
                        "Registration failed. Check your database setup.";
                }
            }
            catch (Exception)
            {
                ViewBag.Message = "Registration failed. Please try again.";
            }

            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {
            if (Session["UserId"] != null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (TempData["AccountMessage"] != null)
            {
                ViewBag.Message = TempData["AccountMessage"];
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrEmpty(password))
            {
                ViewBag.Message = "Enter your email and password.";
                return View();
            }

            try
            {
                string cs = ConfigurationManager
                    .ConnectionStrings["KisanDirectConnection"]
                    .ConnectionString;

                using (SqlConnection con = new SqlConnection(cs))
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT UserId, FullName, Email, PasswordHash, Role " +
                    "FROM dbo.Users WHERE Email = @Email", con))
                {
                    cmd.Parameters.Add("@Email", SqlDbType.VarChar, 150)
                        .Value = email.Trim();

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedHash =
                                reader["PasswordHash"].ToString();

                            if (PasswordHelper.VerifyPassword(
                                password, storedHash))
                            {
                                Session.Clear();

                                Session["UserId"] =
                                    Convert.ToInt32(reader["UserId"]);

                                Session["FullName"] =
                                    reader["FullName"].ToString();

                                Session["Email"] =
                                    reader["Email"].ToString();

                                Session["Role"] =
                                    reader["Role"].ToString();

                                return RedirectToAction("Index", "Home");
                            }
                        }
                    }
                }

                ViewBag.Message = "Invalid email or password.";
            }
            catch (Exception)
            {
                ViewBag.Message = "Login failed. Please try again.";
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login");
        }
    }
}