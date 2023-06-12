using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WORKFLOW.Models
{
    public class Loginmodel
    {
        
       
            [Required(ErrorMessage = "Username is required")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Password is required")]
            public string Password { get; set; }
        public bool IsAdmin { get; set; }


        public string Subject { get; internal set; }
        public string Message { get; internal set; }
    }
}