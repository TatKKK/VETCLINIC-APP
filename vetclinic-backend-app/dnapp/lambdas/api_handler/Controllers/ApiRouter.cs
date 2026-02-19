using Amazon.Lambda.APIGatewayEvents;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Function.Controllers
{
    /// <summary>
    /// Routes API Gateway requests to the appropriate controller by path and HTTP method.
    /// </summary>
    public class ApiRouter
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly AuthController _authController;

        public ApiRouter(AuthController authController)
        {
            _authController = authController ?? throw new ArgumentNullException(nameof(authController));
        }

        public async Task<APIGatewayProxyResponse> RouteAsync(APIGatewayProxyRequest request)
        {
            var path = request.Path?.TrimEnd('/') ?? "";
            var method = request.HttpMethod?.ToUpperInvariant() ?? "";

            if (method == "OPTIONS")
                return OkCors();

            if (path.StartsWith("/auth", StringComparison.OrdinalIgnoreCase))
                return await RouteAuthAsync(path, method, request).ConfigureAwait(false);

            return NotFound();
        }

        private static APIGatewayProxyResponse OkCors()
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = 204,
                Headers = new Dictionary<string, string>
                {
                    ["Access-Control-Allow-Origin"] = "*",
                    ["Access-Control-Allow-Headers"] = "Content-Type,Authorization",
                    ["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS"
                }
            };
        }

        private async Task<APIGatewayProxyResponse> RouteAuthAsync(string path, string method, APIGatewayProxyRequest request)
        {
            if (path.Equals("/auth/register", StringComparison.OrdinalIgnoreCase))
            {
                if (method == "POST") return await _authController.RegisterAsync(request).ConfigureAwait(false);
                return MethodNotAllowed("POST");
            }

            if (path.Equals("/auth/login", StringComparison.OrdinalIgnoreCase))
            {
                if (method == "POST") return await _authController.LoginAsync(request).ConfigureAwait(false);
                return MethodNotAllowed("POST");
            }

            if (path.Equals("/auth/logout", StringComparison.OrdinalIgnoreCase))
            {
                if (method == "POST") return await _authController.LogoutAsync(request).ConfigureAwait(false);
                return MethodNotAllowed("POST");
            }

            return NotFound();
        }

        private static APIGatewayProxyResponse NotFound()
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["Access-Control-Allow-Origin"] = "*"
                },
                Body = JsonSerializer.Serialize(new { Error = "Not Found", Message = "The requested resource was not found." }, JsonOptions)
            };
        }

        private static APIGatewayProxyResponse MethodNotAllowed(string allow)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.MethodNotAllowed,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["Allow"] = allow,
                    ["Access-Control-Allow-Origin"] = "*"
                },
                Body = JsonSerializer.Serialize(new { Error = "Method Not Allowed" }, JsonOptions)
            };
        }
    }
}
