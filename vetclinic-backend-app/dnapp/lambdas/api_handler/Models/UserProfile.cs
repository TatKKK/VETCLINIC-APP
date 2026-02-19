using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Function.Models
{
    public sealed class UserProfile
    {
        // Partition key
        public string Pk { get; set; } = string.Empty;  // e.g. "USER#<sub>"

        // Attributes
        public string Sub { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = UserRoles.Customer;
        public string CreatedAt { get; set; } = string.Empty; // ISO string
    }

}
