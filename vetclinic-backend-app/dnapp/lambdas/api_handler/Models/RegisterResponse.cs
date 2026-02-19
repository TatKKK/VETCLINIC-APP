using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Function.Models
{
    public sealed class RegisterResponse
    {
        public string UserId { get; set; } = string.Empty;   // Cognito sub if available
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = UserRoles.Client;   // default; may be assigned by criteria
        public bool ConfirmationRequired { get; set; }
        public string Message { get; set; } = string.Empty;
    }

}
