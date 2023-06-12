using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WORKFLOW.Models;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Net;
using System.Text;



namespace WORKFLOW.Controllers
{
    public class AccountController : Controller
    {
        private readonly WorkFlowEntities2 dbContext;

        public AccountController()
        {
            dbContext = new WorkFlowEntities2();
        }

        protected override void Dispose(bool disposing)
        {
            dbContext.Dispose();
            base.Dispose(disposing);
        }

        public ActionResult Index()
        {
            var user = Session["User"] as User;
            if (user != null)
            {
                // Retrieve leave requests for the user
                var leaveRequests = dbContext.LeaveRequests.Where(r => r.fk_user_id == user.user_id).ToList();

                ViewBag.User = user;
                ViewBag.LeaveRequests = leaveRequests;

                return View(user);
            }
            else
            {
                // User is not logged in, redirect to the Login action
                return RedirectToAction("Login");
            }


        }

        public new ActionResult Request()
        {
            return View();
        }
        // Assuming you have a database context called 'DbContext' and a 'LeaveRequest' table/entity
        public ActionResult Admin()
        {
            // Check if the user is logged in as an admin
            if (Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin")
            {
                // Render the admin page
                using (var dbContext = new WorkFlowEntities2())
                {
                    var leaveRequests = dbContext.LeaveRequests.ToList();
                    var model = new List<Tuple<WORKFLOW.User, WORKFLOW.LeaveRequest>>();

                    foreach (var request in leaveRequests)
                    {
                        // Assuming you have a user object associated with each leave request
                        var user = dbContext.Users.FirstOrDefault(u => u.user_id == request.fk_user_id);
                        var tuple = new Tuple<WORKFLOW.User, WORKFLOW.LeaveRequest>(user, request);
                        model.Add(tuple);
                    }

                    return View(model);
                }
            }

            // If the user is not logged in as an admin, redirect to the login page
            return RedirectToAction("Login");
        }

        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ForgotPassword(ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                // If model validation fails, return the view with validation errors
                return View(model);
            }

            try
            {
                // Generate a random password
                string newPassword = GenerateRandomPassword();

                // Send the new password to the user's email address
                bool emailSent = SendPasswordResetEmail(
                    model.Email,
                    newPassword,
                    "smtpout.secureserver.net", // Your SMTP server address
                    587, // The port number of your SMTP server
                    "deepak.patidar@averybit.in", // Your email address
                    "Deep1221@", // Your email password
                    true // Enable SSL/TLS
                );

                if (emailSent)
                {
                    // Update the user's password with the new one (you may have a separate logic for this)
                    UpdateUserPassword(model.Email, newPassword);

                    // Redirect to a page indicating that the reset email has been sent
                    return RedirectToAction("ResetEmailSent");
                }
                else
                {
                    ModelState.AddModelError("", "Failed to send the password reset email. Please try again later.");
                }
            }
            catch (Exception ex)
            {
                // Handle email sending error
                ModelState.AddModelError("", "Failed to send the password reset email: " + ex.Message);
            }

            // If email sending fails, return the view with appropriate errors
            return View(model);
        }
        public ActionResult ResetEmailSent()
        {
            return View();
        }

        private void UpdateUserPassword(string email, string newPassword)
        {
            // Assuming you are using a database, you need to establish a connection and execute the update query.
         // Replace with your actual connection string
            string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionString"]?.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Open the connection
                connection.Open();

                // Prepare the update query
                string updateQuery = "UPDATE Users SET Password = @NewPassword WHERE Email = @Email";

                using (SqlCommand command = new SqlCommand(updateQuery, connection))
                {
                    // Set the parameters
                    command.Parameters.AddWithValue("@NewPassword", newPassword);
                    command.Parameters.AddWithValue("@Email", email);

                    // Execute the update query
                    command.ExecuteNonQuery();
                }

                // Close the connection
                connection.Close();
            }
        }


        private string GenerateRandomPassword()
        {
            const string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            Random random = new Random();
            StringBuilder password = new StringBuilder();

            for (int i = 0; i < 8; i++)
            {
                int index = random.Next(allowedChars.Length);
                password.Append(allowedChars[index]);
            }

            return password.ToString();
        }

        private bool SendPasswordResetEmail(string email, string password, string smtpServer, int smtpPort, string smtpUsername, string smtpPassword, bool enableSsl)
        {
            try
            {
                // Set up the email message
                MailMessage message = new MailMessage();
                message.From = new MailAddress("deepak.patidar@averybit.in"); // Your email address
                message.To.Add(new MailAddress(email));
                message.Subject = "Password Reset";
                message.Body = $"Your new password is: {password}";

                // Configure the SMTP client
                SmtpClient smtpClient = new SmtpClient("smtpout.secureserver.net"); // Your SMTP server address
                smtpClient.Port = smtpPort; // The port number of your SMTP server
                smtpClient.Credentials = new NetworkCredential("deepak.patidar@averybit.in", "Deep1221@"); // Your email credentials
                smtpClient.EnableSsl = enableSsl;

                // Send the email
                smtpClient.Send(message);

                return true;
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during email sending
                // You can log the error or perform any desired actions
                return false;
            }
        }


        [HttpPost]
        public ActionResult UpdateStatus(int leaveRequestId, string status)
        {
            using (var dbContext = new WorkFlowEntities2())
            {
                var leaveRequest = dbContext.LeaveRequests.FirstOrDefault(r => r.leave_request_Id == leaveRequestId);

                if (leaveRequest != null)
                {
                    // Update the leave request status
                    leaveRequest.leave_status = status;
                    dbContext.SaveChanges();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Leave request not found." });
                }
            }
        }

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(Loginmodel model)
        {
            if (ModelState.IsValid)
            {
                // Check if the entered credentials match the admin credentials
                if (model.Username == "admin" && model.Password == "12345")
                {
                    // Successful login as admin
                    // Store the user role in Session for later use
                    Session["UserRole"] = "Admin";

                    // Redirect to the admin page
                    return RedirectToAction("Admin", "Account");
                }
                else
                {
                    // Perform authentication logic using the DbContext for normal users
                    var user = dbContext.Users.FirstOrDefault(u => u.email == model.Username && u.password == model.Password);

                    if (user != null)
                    {
                        // Successful login as a normal user
                        // Store the user in Session for later use
                        Session["User"] = user;

                        // Redirect to the index page after successful login
                        return RedirectToAction("Index", "Account");
                    }
                    else
                    {
                        // Invalid credentials
                        ModelState.AddModelError("", "Invalid username or password");
                    }
                }
            }

            // If there are validation errors, return the view with the model
            return View(model);
        }



        private List<LeaveRequest> GetLeaveRequestsFromAPI()
        {
            try
            {
                var apiUrl = "<https://localhost:44398/api/LeaveRequest>"; // Replace with the actual API endpoint URL
                using (HttpClient client = new HttpClient())
                {
                    var response = client.GetAsync(apiUrl).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var leaveRequestsJson = response.Content.ReadAsStringAsync().Result;
                        var leaveRequests = Newtonsoft.Json.JsonConvert.DeserializeObject<List<LeaveRequest>>(leaveRequestsJson);
                        return leaveRequests;
                    }
                    else
                    {
                        // Handle the API error response
                        // You can log the error or handle it as per your requirements
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occurred during the API request
                // You can log the exception or handle it as per your requirements
            }

            return new List<LeaveRequest>(); // Return an empty list if there was an error or no leave requests were retrieved
        }


        [HttpPost]
        public async Task<ActionResult> SendEmail(EmailModel model, HttpPostedFileBase attachment)
        {
            if (!ModelState.IsValid)
            {
                // If model validation fails, return the view with validation errors
                return View(model);
            }

            try
            {
                // Retrieve the logged-in user
                var loggedInUser = Session["User"] as User;

                // Send email using the user and other relevant data
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(loggedInUser?.email);
                mailMessage.To.Add(model.EmailTo);

                // Add CC if provided
                if (!string.IsNullOrEmpty(model.CC))
                {
                    mailMessage.CC.Add(model.CC);
                }

                mailMessage.Subject = model.Subject;
                mailMessage.Body = model.Message;

                // Add attachment if provided
                if (attachment != null && attachment.ContentLength > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        attachment.InputStream.CopyTo(memoryStream);
                        byte[] fileBytes = memoryStream.ToArray();

                        // Create the attachment
                        Attachment emailAttachment = new Attachment(new MemoryStream(fileBytes), attachment.FileName);
                        mailMessage.Attachments.Add(emailAttachment);
                    }
                }

                SmtpClient smtpClient = new SmtpClient("smtpout.secureserver.net", 587); // Use GoDaddy SMTP server and port
                smtpClient.Credentials = new System.Net.NetworkCredential("deepak.patidar@averybit.in", "Deep1221@"); // Replace with your GoDaddy email and password
                smtpClient.EnableSsl = true; // Enable SSL/TLS

                smtpClient.Send(mailMessage);

                // Email sent successfully
                TempData["SuccessMessage"] = "Email sent successfully";

                // Save the data in the LeaveRequest table
                string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionString"]?.ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string insertQuery = "INSERT INTO LeaveRequest (fk_user_id, start_date, end_date, leave_status, Description) " +
                                         "VALUES (@UserId, @StartDate, @EndDate, @LeaveStatus, @Description)";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        // Retrieve the fk_user_id for the logged-in user
                        int staticUserId = loggedInUser?.user_id ?? 0; // Replace 'user_id' with the actual property name

                        command.Parameters.AddWithValue("@UserId", staticUserId);
                        command.Parameters.AddWithValue("@StartDate", model.FromDate);
                        command.Parameters.AddWithValue("@EndDate", model.ToDate);
                        command.Parameters.AddWithValue("@LeaveStatus", "Pending");
                        command.Parameters.AddWithValue("@Description", model.Subject);

                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                return RedirectToAction("Index", "Account");
            }
            catch (Exception ex)
            {
                // Handle email sending error
                ModelState.AddModelError("", "Failed to send email: " + ex.Message);
            }

            // Redirect to Account/Index in case of error
            return RedirectToAction("Index", "Account");
        }

        [HttpGet]
        public ActionResult Register()
        {
            // Populate the list of positions and assign it to the ViewBag
            var positionList = GetPositionList();
            ViewBag.PositionList = positionList;

            return View();
        }

        [HttpPost]
        public ActionResult Register(Regis model)
        {
            if (ModelState.IsValid)
            {
                // Check if the email already exists
                var existingUser = dbContext.Users.FirstOrDefault(u => u.email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Email already exists. Please use a different email.");
                    ViewBag.PositionList = GetPositionList(); // Populate the position list again
                    ViewBag.ErrorMessage = "Email already exists. Please use a different email."; // Set the error message in ViewBag
                    return View(model);
                }

                try
                {
                    // Generate a unique user ID (incremented by one from the last user ID)
                    int lastUserId = dbContext.Users.OrderByDescending(u => u.user_id).Select(u => u.user_id).FirstOrDefault();
                    int newUserId = lastUserId + 1;

                    // Create a new User object with the registration data
                    User newUser = new User
                    {
                        user_id = newUserId,
                        name = model.Name,
                        email = model.Email,
                        password = model.Password,
                        position = model.Position,
                        isactive_status = model.IsActiveStatus
                    };

                    // Save the new user to the database
                    dbContext.Users.Add(newUser);
                    dbContext.SaveChanges();

                    // Redirect to the login page after successful registration
                    return RedirectToAction("Login", "Account");
                }
                catch (Exception ex)
                {
                    // Handle database error
                    ModelState.AddModelError("", "Registration failed. Please try again later.");
                }
            }

            // Populate the list of positions and assign it to the ViewBag
            var positionList = GetPositionList();
            ViewBag.PositionList = positionList;

            // If there are validation errors or the registration fails, return the view with the model
            return View(model);
        }

        private List<SelectListItem> GetPositionList()
        {
            return new List<SelectListItem>
    {
        new SelectListItem { Value = "Trainee", Text = "Trainee" },
        new SelectListItem { Value = "Senior Software Engineer", Text = "Senior Software Engineer" },
        new SelectListItem { Value = "Intern", Text = "Intern" },
        new SelectListItem { Value = "HR", Text = "HR" },
        new SelectListItem { Value = "Management", Text = "Management" }
        // Add more positions as needed
    };
        }



    }
}






