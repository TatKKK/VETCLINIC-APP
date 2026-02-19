using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Function.Environment
{
   
        public static class Env
        {
            public static string UserProfilesTable => GetEnvironmentVariable("USER_PROFILES_TABLE");
            public static string CognitoClientId => GetEnvironmentVariable("COGNITO_CLIENT_ID");
            public static string CognitoUserPoolId => GetEnvironmentVariable("COGNITO_USER_POOL_ID");
            public static string AwsRegion => GetEnvironmentVariable("AWS_REGION");

            private static string GetEnvironmentVariable(string variable)
            {
            var value = System.Environment.GetEnvironmentVariable(variable);
                if (string.IsNullOrEmpty(value))
                {
                    throw new InvalidOperationException($"Environment variable '{variable}' is not set.");
                }
                return value;
            }
        }
    }

