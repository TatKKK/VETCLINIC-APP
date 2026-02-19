using Amazon.Lambda.APIGatewayEvents;
using Function.Models;
using Function.Services.Abstract;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Function.Controllers
{
    public class AuthController
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        public async Task<APIGatewayProxyResponse> RegisterAsync(APIGatewayProxyRequest request)
        {
            var body = Deserialize<RegisterRequest>(request.Body);
            if (body == null)
                return BadRequest(new AuthErrorResponse
                {
                    Error = "Bad Request",
                    Code = AuthErrorCodes.InvalidInput,
                    Message = "Invalid or missing request body."
                });

            var response = await _authService.RegisterAsync(body).ConfigureAwait(false);

            if (string.IsNullOrEmpty(response.UserId))
            {
                return BadRequest(new AuthErrorResponse
                {
                    Error = "Bad Request",
                    Code = AuthErrorCodes.InvalidInput,
                    Message = response.Message
                });
            }

            return Json(201, response);
        }

        public async Task<APIGatewayProxyResponse> LoginAsync(APIGatewayProxyRequest request)
        {
            var body = Deserialize<LoginRequest>(request.Body);
            if (body == null || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
                return BadRequest(new AuthErrorResponse
                {
                    Error = "Bad Request",
                    Code = AuthErrorCodes.InvalidInput,
                    Message = "Email and password are required."
                });

            try
            {
                var response = await _authService.LoginAsync(body).ConfigureAwait(false);
                return Json(200, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new AuthErrorResponse
                {
                    Error = "Bad Request",
                    Code = AuthErrorCodes.InvalidInput,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException)
            {
                return Unauthorized(new AuthErrorResponse
                {
                    Error = "Unauthorized",
                    Code = AuthErrorCodes.InvalidCredentials,
                    Message = "Invalid email or password."
                });
            }
            catch (Exception ex) when (ex.Message.Contains("NotAuthorizedException") || ex.Message.Contains("UserNotFoundException"))
            {
                return Unauthorized(new AuthErrorResponse
                {
                    Error = "Unauthorized",
                    Code = AuthErrorCodes.InvalidCredentials,
                    Message = "Invalid email or password."
                });
            }
        }

        public async Task<APIGatewayProxyResponse> LogoutAsync(APIGatewayProxyRequest request)
        {
            string? accessToken = null;
            if (request.Headers != null && TryGetAuthBearer(request.Headers, out var token))
                accessToken = token;
            if (string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(request.Body))
            {
                var body = Deserialize<LogoutRequest>(request.Body);
                accessToken = body?.AccessToken;
            }

            await _authService.LogoutAsync(accessToken ?? string.Empty).ConfigureAwait(false);
            return Json(200, new { Message = "Logged out successfully." });
        }

        private static T? Deserialize<T>(string? body) where T : class
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(body, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private static APIGatewayProxyResponse Json(int statusCode, object body)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = statusCode,
                Headers = CorsHeaders(),
                Body = JsonSerializer.Serialize(body, JsonOptions)
            };
        }

        private static APIGatewayProxyResponse BadRequest(AuthErrorResponse error)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Headers = CorsHeaders(),
                Body = JsonSerializer.Serialize(error, JsonOptions)
            };
        }

        private static APIGatewayProxyResponse Unauthorized(AuthErrorResponse error)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Headers = CorsHeaders(),
                Body = JsonSerializer.Serialize(error, JsonOptions)
            };
        }

        private static bool TryGetAuthBearer(IDictionary<string, string>? headers, out string? bearerToken)
        {
            bearerToken = null;
            if (headers == null) return false;
            foreach (var kv in headers)
            {
                if (kv.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) && kv.Value?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                {
                    bearerToken = kv.Value.Substring(7).Trim();
                    return true;
                }
            }
            return false;
        }

        private static Dictionary<string, string> CorsHeaders()
        {
            return new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Access-Control-Allow-Origin"] = "*",
                ["Access-Control-Allow-Headers"] = "Content-Type,Authorization"
            };
        }
    }

    internal sealed class LogoutRequest
    {
        public string? AccessToken { get; set; }
    }
}
