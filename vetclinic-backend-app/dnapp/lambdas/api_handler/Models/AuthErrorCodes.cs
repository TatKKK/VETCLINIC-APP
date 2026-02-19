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
        public const string AccountLocked = "ACCOUNT_LOCKED";
        public const string InvalidInput = "INVALID_INPUT";
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string NotAuthorized = "NOT_AUTHORIZED";

        // Additional codes for registration
        public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
        public const string WeakPassword = "WEAK_PASSWORD";
        public const string PasswordMismatch = "PASSWORD_MISMATCH";
        public const string InvalidEmailFormat = "INVALID_EMAIL_FORMAT";
        public const string InvalidPhoneFormat = "INVALID_PHONE_FORMAT";
    }


}
