using System.Collections.Generic;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using WORKFLOW.Models;

namespace WORKFLOW.DAL
{
    public class AccountDAL
    {
        private readonly string connectionString;

        public AccountDAL()
        {
            // Retrieve the connection string from the configuration file
            connectionString = ConfigurationManager.ConnectionStrings["MyConnectionString"]?.ConnectionString;
        }

        public User AuthenticateUser(string email, string password)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM Users WHERE Email = @Email AND Password = @Password";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Password", password);

                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Map the reader data to a User object
                            User user = new User
                            {
                                user_id = (int)reader["user_id"],
                                name = reader["name"].ToString(),
                                email = reader["email"].ToString(),
                                password = reader["password"].ToString(),
                                position = reader["position"].ToString(),
                                isactive_status = (bool)reader["isactive_status"]
                            };

                            return user;
                        }
                    }
                }
            }

            return null;
        }

        public LeaveRequest[] GetLeaveRequestsByUserId(int userId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM LeaveRequest WHERE fk_user_id = @UserId";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // Create a list to store leave requests
                        List<LeaveRequest> leaveRequests = new List<LeaveRequest>();

                        while (reader.Read())
                        {
                            // Map the reader data to a LeaveRequest object
                            LeaveRequest leaveRequest = new LeaveRequest
                            {
                                leave_request_Id = (int)reader["leave_request_Id"],
                                fk_user_id = (int)reader["fk_user_id"],
                                start_date = (DateTime)reader["start_date"],
                                end_date = (DateTime)reader["end_date"],
                                leave_status = reader["leave_status"].ToString(),
                                Description = reader["Description"].ToString()
                            };

                            leaveRequests.Add(leaveRequest);
                        }

                        return leaveRequests.ToArray();
                    }
                }
            }
        }
    }
}
