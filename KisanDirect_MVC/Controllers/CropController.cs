using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace KisanDirect_MVC.Controllers
{
    public class CropController : Controller
    {
        private string ConnectionString
        {
            get
            {
                return ConfigurationManager
                .ConnectionStrings["KisanDirectConnection"]
                .ConnectionString;
            }
        }

        // Check whether user is logged in
        private bool IsLoggedIn()
        {
            return Session["UserId"] != null;
        }

        // Check whether logged-in user is Owner
        private bool IsOwner()
        {
            return IsLoggedIn() &&
                   Session["Role"] != null &&
                   Session["Role"].ToString() == "Owner";
        }

        // Check whether Owner or crop's own Farmer can manage it
        private bool CanManageCrop(int id)
        {
            if (!IsLoggedIn())
                return false;

            if (IsOwner())
                return true;

            if (Session["Role"] == null ||
                Session["Role"].ToString() != "Farmer")
                return false;

            int userId = Convert.ToInt32(Session["UserId"]);

            using (SqlConnection con =
                new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.Crops " +
                "WHERE CropId = @CropId AND FarmerId = @FarmerId",
                con))
            {
                cmd.Parameters.Add("@CropId", SqlDbType.Int).Value = id;
                cmd.Parameters.Add("@FarmerId", SqlDbType.Int).Value = userId;

                con.Open();

                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        // Marketplace: Search and sorting
        public ActionResult Index(string search, string sortOrder)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Register", "Account");

            DataTable crops = new DataTable();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentSort = sortOrder;

            try
            {
                using (SqlConnection con =
                    new SqlConnection(ConnectionString))
                {
                    string orderBy = "CropId DESC";

                    if (sortOrder == "price_low")
                        orderBy = "Price ASC";
                    else if (sortOrder == "price_high")
                        orderBy = "Price DESC";

                    string query =
                        "SELECT CropId, CropName, Price, Quantity, " +
                        "Location, MobileNumber, DateAdded, FarmerId " +
                        "FROM dbo.Crops " +
                        "WHERE (@Search = '' " +
                        "OR CropName LIKE @Pattern " +
                        "OR Location LIKE @Pattern) " +
                        "ORDER BY " + orderBy;

                    using (SqlCommand cmd =
                        new SqlCommand(query, con))
                    {
                        string searchText =
                            string.IsNullOrWhiteSpace(search)
                            ? ""
                            : search.Trim();

                        cmd.Parameters.Add(
                            "@Search", SqlDbType.NVarChar, 100
                        ).Value = searchText;

                        cmd.Parameters.Add(
                            "@Pattern", SqlDbType.NVarChar, 202
                        ).Value = "%" + searchText + "%";

                        using (SqlDataAdapter adapter =
                            new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(crops);
                        }
                    }
                }
            }
            catch (Exception)
            {
                ViewBag.Message =
                    "Unable to load crops. Please check database connection.";
            }

            return View(crops);
        }

        // Delete: Owner or crop's own Farmer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Register", "Account");

            if (id <= 0)
            {
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.BadRequest);
            }

            try
            {
                if (!CanManageCrop(id))
                {
                    return new HttpStatusCodeResult(
                        System.Net.HttpStatusCode.Forbidden);
                }

                using (SqlConnection con =
                    new SqlConnection(ConnectionString))
                using (SqlCommand cmd = new SqlCommand(
                    IsOwner()
                    ? "DELETE FROM dbo.Crops WHERE CropId = @CropId"
                    : "DELETE FROM dbo.Crops WHERE CropId = @CropId AND FarmerId = @FarmerId",
                    con))
                {
                    cmd.Parameters.Add(
                        "@CropId", SqlDbType.Int).Value = id;

                    if (!IsOwner())
                    {
                        cmd.Parameters.Add(
                            "@FarmerId", SqlDbType.Int
                        ).Value = Convert.ToInt32(Session["UserId"]);
                    }

                    con.Open();
                    int rows = cmd.ExecuteNonQuery();

                    TempData["Message"] = rows > 0
                        ? "Crop deleted successfully."
                        : "Crop not found or you cannot manage it.";
                }
            }
            catch (Exception)
            {
                TempData["Message"] =
                    "Unable to delete crop. Please try again.";
            }

            return RedirectToAction("Index");
        }

        // Edit: Open edit page for Owner or crop's Farmer
        [HttpGet]
        public ActionResult Edit(int id)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Register", "Account");

            if (id <= 0)
            {
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.BadRequest);
            }

            try
            {
                if (!CanManageCrop(id))
                {
                    return new HttpStatusCodeResult(
                        System.Net.HttpStatusCode.Forbidden);
                }

                DataTable crop = new DataTable();

                using (SqlConnection con =
                    new SqlConnection(ConnectionString))
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT CropId, CropName, Price, Quantity, " +
                    "Location, MobileNumber " +
                    "FROM dbo.Crops WHERE CropId = @Id", con))
                {
                    cmd.Parameters.Add(
                        "@Id", SqlDbType.Int).Value = id;

                    using (SqlDataAdapter adapter =
                        new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(crop);
                    }
                }

                if (crop.Rows.Count == 0)
                    return HttpNotFound();

                return View(crop.Rows[0]);
            }
            catch (Exception)
            {
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.InternalServerError);
            }
        }

        // Save edited crop: Owner or its Farmer only
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            int id,
            string cropName,
            decimal price,
            decimal quantity,
            string location,
            string mobileNumber)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Register", "Account");

            if (id <= 0)
            {
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrWhiteSpace(cropName) ||
                price <= 0 ||
                quantity <= 0 ||
                string.IsNullOrWhiteSpace(location))
            {
                TempData["Message"] =
                    "Enter valid crop details. Price and quantity must be greater than zero.";

                return RedirectToAction("Index");
            }

            try
            {
                if (!CanManageCrop(id))
                {
                    return new HttpStatusCodeResult(
                        System.Net.HttpStatusCode.Forbidden);
                }

                string query = IsOwner()
                    ? "UPDATE dbo.Crops SET CropName=@Name, Price=@Price, " +
                      "Quantity=@Quantity, Location=@Location, MobileNumber=@Mobile " +
                      "WHERE CropId=@Id"
                    : "UPDATE dbo.Crops SET CropName=@Name, Price=@Price, " +
                      "Quantity=@Quantity, Location=@Location, MobileNumber=@Mobile " +
                      "WHERE CropId=@Id AND FarmerId=@FarmerId";

                using (SqlConnection con =
                    new SqlConnection(ConnectionString))
                using (SqlCommand cmd =
                    new SqlCommand(query, con))
                {
                    cmd.Parameters.Add(
                        "@Name", SqlDbType.VarChar, 100
                    ).Value = cropName.Trim();

                    SqlParameter priceParameter =
                        cmd.Parameters.Add("@Price", SqlDbType.Decimal);
                    priceParameter.Precision = 10;
                    priceParameter.Scale = 2;
                    priceParameter.Value = price;

                    SqlParameter quantityParameter =
                        cmd.Parameters.Add("@Quantity", SqlDbType.Decimal);
                    quantityParameter.Precision = 10;
                    quantityParameter.Scale = 2;
                    quantityParameter.Value = quantity;

                    cmd.Parameters.Add(
                        "@Location", SqlDbType.VarChar, 100
                    ).Value = location.Trim();

                    cmd.Parameters.Add(
                        "@Mobile", SqlDbType.VarChar, 10
                    ).Value = string.IsNullOrWhiteSpace(mobileNumber)
                        ? (object)DBNull.Value
                        : mobileNumber.Trim();

                    cmd.Parameters.Add(
                        "@Id", SqlDbType.Int).Value = id;

                    if (!IsOwner())
                    {
                        cmd.Parameters.Add(
                            "@FarmerId", SqlDbType.Int
                        ).Value = Convert.ToInt32(Session["UserId"]);
                    }

                    con.Open();
                    int rows = cmd.ExecuteNonQuery();

                    TempData["Message"] = rows > 0
                        ? "Crop updated successfully!"
                        : "Crop not found or you cannot manage it.";
                }
            }
            catch (Exception)
            {
                TempData["Message"] =
                    "Unable to update crop. Please check the details and try again.";
            }

            return RedirectToAction("Index");
        }
    }

}