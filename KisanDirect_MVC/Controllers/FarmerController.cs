using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace KisanDirect_MVC.Controllers
{
    public class FarmerController : Controller
    {
        [HttpGet]
        public ActionResult Create()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Register", "Account");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            string cropName,
            decimal? price,
            decimal? quantity,
            string location,
            string mobileNumber)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Register", "Account");
            }

            if (string.IsNullOrWhiteSpace(cropName) ||
                !price.HasValue || price.Value <= 0 ||
                !quantity.HasValue || quantity.Value <= 0 ||
                string.IsNullOrWhiteSpace(location) ||
                string.IsNullOrWhiteSpace(mobileNumber) ||
                !Regex.IsMatch(mobileNumber, @"^[0-9]{10}$"))
            {
                ViewBag.Message =
                    "Please fill all fields correctly. Mobile number must contain 10 digits.";

                return View();
            }

            try
            {
                var setting =
                    ConfigurationManager.ConnectionStrings[
                        "KisanDirectConnection"];

                if (setting == null)
                {
                    ViewBag.Message =
                        "Database connection string was not found.";

                    return View();
                }

                int farmerId = Convert.ToInt32(Session["UserId"]);

                using (SqlConnection con =
                    new SqlConnection(setting.ConnectionString))
                {
                    string query =
                        "INSERT INTO dbo.Crops " +
                        "(CropName, Price, Quantity, Location, " +
                        "MobileNumber, FarmerId) " +
                        "VALUES (@CropName, @Price, @Quantity, " +
                        "@Location, @MobileNumber, @FarmerId)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@CropName", SqlDbType.VarChar, 100
                        ).Value = cropName.Trim();

                        SqlParameter priceParam =
                            cmd.Parameters.Add("@Price", SqlDbType.Decimal);
                        priceParam.Precision = 10;
                        priceParam.Scale = 2;
                        priceParam.Value = price.Value;

                        SqlParameter quantityParam =
                            cmd.Parameters.Add("@Quantity", SqlDbType.Decimal);
                        quantityParam.Precision = 10;
                        quantityParam.Scale = 2;
                        quantityParam.Value = quantity.Value;

                        cmd.Parameters.Add(
                            "@Location", SqlDbType.VarChar, 100
                        ).Value = location.Trim();

                        cmd.Parameters.Add(
                            "@MobileNumber", SqlDbType.VarChar, 10
                        ).Value = mobileNumber;

                        cmd.Parameters.Add(
                            "@FarmerId", SqlDbType.Int
                        ).Value = farmerId;

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                ViewBag.Message =
                    "Crop saved successfully with your account!";

                ViewBag.CropName = cropName;
                ViewBag.Price = price.Value;
                ViewBag.Quantity = quantity.Value;
                ViewBag.Location = location;
                ViewBag.MobileNumber = mobileNumber;

                ModelState.Clear();
            }
            catch (Exception)
            {
                ViewBag.Message =
                    "Crop save failed. Please check your database and try again.";
            }

            return View();
        }
    }

}