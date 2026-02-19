using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Function.Environment;
using Function.Models;
using Function.Repositories.Abstract;
using Function.Services.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Function.Services.Concrete
{
    public class AuthService : IAuthService
    {
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly IAmazonCognitoIdentityProvider _cognitoClient;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IUserService _userService;

        public AuthService(
            IAmazonCognitoIdentityProvider cognitoClient,
            IUserProfileRepository userProfileRepository,
            IUserService userService)
        {
            _cognitoClient = cognitoClient ?? throw new ArgumentNullException(nameof(cognitoClient));
            _userProfileRepository = userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        /// <summary>
        /// Validates registration input: required fields, email format, password strength, confirm password match.
        /// </summary>
        private static bool IsValidRegistration(RegisterRequest request)
        {
            if (request == null) return false;
            if (string.IsNullOrWhiteSpace(request.FirstName)) return false;
            if (string.IsNullOrWhiteSpace(request.LastName)) return false;
            if (string.IsNullOrWhiteSpace(request.Email)) return false;
            if (string.IsNullOrWhiteSpace(request.Password)) return false;
            if (!EmailRegex.IsMatch(request.Email.Trim())) return false;
            if (request.Password != request.ConfirmPassword) return false;
            if (request.Password.Length < 8) return false;
            return true;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // 1. Validate input
                if (!IsValidRegistration(request))
                    return new RegisterResponse { Message = "Invalid input data" };

                // 2. Check email uniqueness (DynamoDB profile or Cognito will reject duplicate)
                var existing = await _userProfileRepository.GetByEmailAsync(request.Email).ConfigureAwait(false);
                if (existing != null)
                    return new RegisterResponse { Message = "An account with this email already exists." };

                // 3. Create user in Cognito
                var signUpRequest = new SignUpRequest
                {
                    ClientId = Env.CognitoClientId,
                    Username = request.Email,
                    Password = request.Password,
                    UserAttributes = new List<AttributeType>
                {
                    new() { Name = "email", Value = request.Email },
                    new() { Name = "given_name", Value = request.FirstName },
                    new() { Name = "family_name", Value = request.LastName },
                    new() { Name = "phone_number", Value = request.PhoneNumber }
                }
                };

                var cognitoResponse = await _cognitoClient.SignUpAsync(signUpRequest).ConfigureAwait(false);

                // 4. Determine user role (automatic: list or default Client)
                string assignedRole = await _userService.DetermineUserRoleAsync(request.Email);

                // 5. Create user profile in DynamoDB (role displayed in profile)
                var userProfile = new UserProfile
                {
                    Pk = $"USER#{cognitoResponse.UserSub}",
                    Sub = cognitoResponse.UserSub,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Role = assignedRole,
                    CreatedAt = DateTime.UtcNow.ToString("O")
                };

                await _userProfileRepository.CreateAsync(userProfile);

                return new RegisterResponse
                {
                    UserId = cognitoResponse.UserSub,
                    Email = request.Email,
                    Role = assignedRole,
                    ConfirmationRequired = !(cognitoResponse.UserConfirmed ?? false),
                    Message = "Registration successful"
                };
            }
            catch (UsernameExistsException)
            {
                return new RegisterResponse { Message = "An account with this email already exists." };
            }
            catch (InvalidPasswordException)
            {
                return new RegisterResponse { Message = "Password does not meet requirements (min 8 chars, upper, lower, number)." };
            }
            catch (Exception)
            {
                return new RegisterResponse { Message = "Registration failed." };
            }
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Email and password are required.");

            var authRequest = new InitiateAuthRequest
            {
                ClientId = Env.CognitoClientId,
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                AuthParameters = new Dictionary<string, string>
                {
                    ["USERNAME"] = request.Email,
                    ["PASSWORD"] = request.Password!
                }
            };

            var authResponse = await _cognitoClient.InitiateAuthAsync(authRequest).ConfigureAwait(false);
            if (authResponse.AuthenticationResult == null)
                throw new InvalidOperationException("Authentication failed.");

            var profile = await _userProfileRepository.GetByEmailAsync(request.Email).ConfigureAwait(false);
            var userInfo = profile != null
                ? new UserInfo { Sub = profile.Sub, Email = profile.Email, Role = profile.Role }
                : new UserInfo { Email = request.Email, Role = UserRoles.Client };

            return new LoginResponse
            {
                IdToken = authResponse.AuthenticationResult.IdToken,
                AccessToken = authResponse.AuthenticationResult.AccessToken,
                RefreshToken = authResponse.AuthenticationResult.RefreshToken ?? string.Empty,
                ExpiresIn = authResponse.AuthenticationResult.ExpiresIn ?? 0,
                TokenType = "Bearer",
                User = userInfo
            };
        }

        public Task<bool> LogoutAsync(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken)) return Task.FromResult(false);
            // Cognito GlobalSignOut invalidates all tokens for the user; optional to call here.
            return Task.FromResult(true);
        }
    }
}
