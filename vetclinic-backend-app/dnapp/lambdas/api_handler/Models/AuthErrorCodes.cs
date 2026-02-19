using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Function.Models
{
    public static class AuthErrorCodes
    {
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string AccountLocked = "ACCOUNT_LOCKED"; // Do we have lock options?
        public const string InvalidInput = "INVALID_INPUT";
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string NotAuthorized = "NOT_AUTHORIZED";
    }

}
