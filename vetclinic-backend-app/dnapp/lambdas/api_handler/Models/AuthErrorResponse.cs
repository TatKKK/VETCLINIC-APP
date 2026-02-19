using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Function.Models
{
    public sealed class AuthErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Message { get; set; }
    }

}
