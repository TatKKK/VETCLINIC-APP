using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Function.Environment;
using Function.Models;
using Function.Repositories.Abstract;

namespace Function.Repositories.Concrete
{
    /// <summary>
    /// DynamoDB implementation of user profile storage.
    /// Table partition key: Pk (String), e.g. "USER#&lt;sub&gt;".
    /// </summary>
    public sealed class UserProfileRepository : IUserProfileRepository
    {
        private const string PkName = "Pk";
        private const string PkPrefix = "USER#";

        private readonly IAmazonDynamoDB _client;
        private readonly string _tableName;

        public UserProfileRepository(IAmazonDynamoDB client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _tableName = Env.UserProfilesTable;
        }

        public static string ToPk(string sub) => PkPrefix + sub;

        public async Task<UserProfile> GetBySubAsync(string sub)
        {
            if (string.IsNullOrWhiteSpace(sub))
                throw new ArgumentException("Sub is required.", nameof(sub));

            var pk = ToPk(sub);
            var request = new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    [PkName] = new AttributeValue { S = pk }
                }
            };

            var response = await _client.GetItemAsync(request).ConfigureAwait(false);
            if (response.Item == null || response.Item.Count == 0)
                throw new InvalidOperationException($"User profile not found for sub: {sub}.");

            return FromItem(response.Item);
        }

        public async Task PutAsync(UserProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            if (string.IsNullOrWhiteSpace(profile.Sub))
                throw new ArgumentException("Profile Sub is required.", nameof(profile));

            var pk = string.IsNullOrWhiteSpace(profile.Pk) ? ToPk(profile.Sub) : profile.Pk;
            var request = new PutItemRequest
            {
                TableName = _tableName,
                Item = ToItem(profile, pk)
            };

            await _client.PutItemAsync(request).ConfigureAwait(false);
        }

        private static UserProfile FromItem(Dictionary<string, AttributeValue> item)
        {
            return new UserProfile
            {
                Pk = item.TryGetValue(PkName, out var pk) ? pk.S : string.Empty,
                Sub = item.TryGetValue("Sub", out var sub) ? sub.S : string.Empty,
                Email = item.TryGetValue("Email", out var email) ? email.S : string.Empty,
                Role = item.TryGetValue("Role", out var role) ? role.S : UserRoles.Customer,
                CreatedAt = item.TryGetValue("CreatedAt", out var createdAt) ? createdAt.S : string.Empty
            };
        }

        private static Dictionary<string, AttributeValue> ToItem(UserProfile profile, string pk)
        {
            return new Dictionary<string, AttributeValue>
            {
                [PkName] = new AttributeValue(pk),
                ["Sub"] = new AttributeValue(profile.Sub),
                ["Email"] = new AttributeValue(profile.Email),
                ["Role"] = new AttributeValue(profile.Role),
                ["CreatedAt"] = new AttributeValue(profile.CreatedAt)
            };
        }
    }
}
