using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace KisanDirect_MVC.Controllers
{
    public class AdminController : Controller
    {
        private bool IsOwner()
        {
            return Session["UserId"] != null &&
                   Session["Role"] != null &&
                   Session["Role"].ToString() == "Owner";
        }

        // ADMIN DASHBOARD
        public ActionResult Index()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            if (!IsOwner())
                return RedirectToAction("Index", "Home");

            string cs = ConfigurationManager
                .ConnectionStrings["KisanDirectConnection"]
                .ConnectionString;

            int totalFarmers = 0;
            int totalCrops = 0;

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.Users WHERE Role = 'Farmer'", con))
                {
                    totalFarmers = Convert.ToInt32(cmd.ExecuteScalar());
                }

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.Crops", con))
                {
                    totalCrops = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

            ViewBag.TotalFarmers = totalFarmers;
            ViewBag.TotalCrops = totalCrops;

            return View();
        }

        // FARMERS LIST
        public ActionResult Users()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            if (!IsOwner())
                return RedirectToAction("Index", "Home");

            string cs = ConfigurationManager
                .ConnectionStrings["KisanDirectConnection"]
                .ConnectionString;

            DataTable users = new DataTable();

            using (SqlConnection con = new SqlConnection(cs))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT UserId, FullName, Email " +
                "FROM dbo.Users WHERE Role = 'Farmer' " +
                "ORDER BY UserId DESC", con))
            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
            {
                adapter.Fill(users);
            }

            return View(users);
        }

        // EDIT FARMER - OPEN PAGE
        [HttpGet]
        public ActionResult EditUser(int id)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            if (!IsOwner())
                return RedirectToAction("Index", "Home");

            string cs = ConfigurationManager
                .ConnectionStrings["KisanDirectConnection"]
                .ConnectionString;

            DataTable user = new DataTable();

            using (SqlConnection con = new SqlConnection(cs))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT UserId, FullName, Email FROM dbo.Users " +
                "WHERE UserId = @Id AND Role = 'Farmer'", con))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(user);
                }
            }

            if (user.Rows.Count == 0)
            {
                TempData["AdminMessage"] = "Farmer account not found.";
                return RedirectToAction("Users");
            }

            return View(user.Rows[0]);
        }

        // EDIT FARMER - SAVE CHANGES
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(int id, string fullName, string email)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            if (!IsOwner())
                return RedirectToAction("Index", "Home");

            fullName = (fullName ?? "").Trim();
            email = (email ?? "").Trim();

            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                fullName.Length > 100 ||
                email.Length > 150)
            {
                TempData["AdminMessage"] =
                    "Enter a name and email within the allowed length.";

                return RedirectToAction("EditUser", new { id = id });
            }

            string cs = ConfigurationManager
                .ConnectionStrings["KisanDirectConnection"]
                .ConnectionString;

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    using (SqlCommand check = new SqlCommand(
                        "SELECT COUNT(*) FROM dbo.Users " +
                        "WHERE Email = @Email AND UserId <> @Id", con))
                    {
                        check.Parameters.Add("@Email", SqlDbType.VarChar, 150)
                            .Value = email;
                        check.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                        if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                        {
                            TempData["AdminMessage"] =
                                "This email is already used by another account.";

                            return RedirectToAction("EditUser", new { id = id });
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE dbo.Users SET FullName = @Name, Email = @Email " +
                        "WHERE UserId = @Id AND Role = 'Farmer'", con))
                    {
                        cmd.Parameters.Add("@Name", SqlDbType.VarChar, 100)
                            .Value = fullName;
                        cmd.Parameters.Add("@Email", SqlDbType.VarChar, 150)
                            .Value = email;
                        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                        int rows = cmd.ExecuteNonQuery();

                        if (rows == 0)
                        {
                            TempData["AdminMessage"] = "Farmer account not found.";
                            return RedirectToAction("Users");
                        }
                    }
                }

                TempData["AdminMessage"] =
                    "Farmer details updated successfully.";
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                    TempData["AdminMessage"] = "This email is already registered.";
                else
                    TempData["AdminMessage"] = "Update failed. Check the database.";
            }
            catch (Exception)
            {
                TempData["AdminMessage"] = "Update failed. Please try again.";
            }

            return RedirectToAction("Users");
        }

        // DELETE FARMER
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int id)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            if (!IsOwner())
                return RedirectToAction("Index", "Home");

            string cs = ConfigurationManager
                .ConnectionStrings["KisanDirectConnection"]
                .ConnectionString;

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand check = new SqlCommand(
                            "SELECT COUNT(*) FROM dbo.Users " +
                            "WHERE UserId = @Id AND Role = 'Farmer'",
                            con, transaction))
                        {
                            check.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                            if (Convert.ToInt32(check.ExecuteScalar()) == 0)
                            {
                                transaction.Rollback();
                                TempData["AdminMessage"] = "Farmer account not found.";
                                return RedirectToAction("Users");
                            }
                        }

                        using (SqlCommand cropCmd = new SqlCommand(
                            "DELETE FROM dbo.Crops WHERE FarmerId = @Id",
                            con, transaction))
                        {
                            cropCmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                            cropCmd.ExecuteNonQuery();
                        }

                        using (SqlCommand userCmd = new SqlCommand(
                            "DELETE FROM dbo.Users " +
                            "WHERE UserId = @Id AND Role = 'Farmer'",
                            con, transaction))
                        {
                            userCmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                            userCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        TempData["AdminMessage"] =
                            "Farmer account and their listed crops deleted.";
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        TempData["AdminMessage"] =
                            "Delete failed. Please check the database.";
                    }
                }
            }

            return RedirectToAction("Users");
        }
    }
}