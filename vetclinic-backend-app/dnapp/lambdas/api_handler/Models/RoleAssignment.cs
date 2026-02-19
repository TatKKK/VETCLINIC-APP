using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Function.Models
{
    public sealed class RoleAssignment
    {
        public string Pk { get; set; } = string.Empty;  // EMAIL#{email}
        public string Email { get; set; } = string.Empty;
        public string AssignedRole { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

}
