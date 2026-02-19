using System;
using System.Threading.Tasks;
using Amazon.CognitoIdentityProvider;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Function.Controllers;
using Function.Repositories.Abstract;
using Function.Repositories.Concrete;
using Function.Services.Abstract;
using Function.Services.Concrete;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SimpleLambdaFunction;

public class Function
{
    private static readonly Lazy<ApiRouter> Router = new Lazy<ApiRouter>(CreateRouter);

    private static ApiRouter CreateRouter()
    {
        var dynamoDb = new AmazonDynamoDBClient();
        var cognito = new AmazonCognitoIdentityProviderClient();

        IUserProfileRepository userProfileRepo = new UserProfileRepository(dynamoDb);
        IRoleAssignmentRepository roleAssignmentRepo = new RoleAssignmentRepository(dynamoDb);
        IUserService userService = new UserService(roleAssignmentRepo, userProfileRepo);
        IAuthService authService = new AuthService(cognito, userProfileRepo, userService);
        var authController = new AuthController(authService);
        return new ApiRouter(authController);
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            return await Router.Value.RouteAsync(request).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Unhandled error: {ex}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Headers = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["Access-Control-Allow-Origin"] = "*"
                },
                Body = System.Text.Json.JsonSerializer.Serialize(new { Error = "Internal Server Error", Message = "An unexpected error occurred." })
            };
        }
    }
}
