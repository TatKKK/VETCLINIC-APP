using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Function.Environment;
using Function.Models;
using Function.Repositories.Abstract;

namespace Function.Repositories.Concrete
{
    public class UserProfileRepository : IUserProfileRepository
    {
        private const string PkName = "Pk";
        private const string PkPrefix = "USER#";
        private const string EmailIndexName = "EmailIndex";

        private readonly IAmazonDynamoDB _dynamoDB;
        private readonly string _tableName;

        public UserProfileRepository(IAmazonDynamoDB dynamoDB)
        {
            _dynamoDB = dynamoDB ?? throw new ArgumentNullException(nameof(dynamoDB));
            _tableName = Env.UserProfilesTable;
        }

        public async Task<UserProfile?> GetBySubAsync(string sub)
        {
            if (string.IsNullOrWhiteSpace(sub)) return null;
            var pk = PkPrefix + sub;
            var req = new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue> { [PkName] = new AttributeValue(pk) }
            };
            var res = await _dynamoDB.GetItemAsync(req).ConfigureAwait(false);
            return res.Item?.Count > 0 ? FromItem(res.Item) : null;
        }

        public async Task<UserProfile?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var req = new QueryRequest
            {
                TableName = _tableName,
                IndexName = EmailIndexName,
                KeyConditionExpression = "Email = :email",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":email"] = new AttributeValue(email.Trim()) }
            };
            var res = await _dynamoDB.QueryAsync(req).ConfigureAwait(false);
            var item = res.Items?.FirstOrDefault();
            return item != null ? FromItem(item) : null;
        }

        public async Task<bool> CreateAsync(UserProfile userProfile)
        {
            if (userProfile == null || string.IsNullOrWhiteSpace(userProfile.Sub)) return false;
            var pk = string.IsNullOrWhiteSpace(userProfile.Pk) ? PkPrefix + userProfile.Sub : userProfile.Pk;
            var req = new PutItemRequest { TableName = _tableName, Item = ToItem(userProfile, pk) };
            await _dynamoDB.PutItemAsync(req).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> UpdateAsync(UserProfile userProfile)
        {
            if (userProfile == null || string.IsNullOrWhiteSpace(userProfile.Pk)) return false;
            var req = new PutItemRequest { TableName = _tableName, Item = ToItem(userProfile, userProfile.Pk) };
            await _dynamoDB.PutItemAsync(req).ConfigureAwait(false);
            return true;
        }

        private static UserProfile FromItem(Dictionary<string, AttributeValue> item)
        {
            string GetS(string key) => item.TryGetValue(key, out var a) ? a.S ?? string.Empty : string.Empty;
            return new UserProfile
            {
                Pk = GetS(PkName),
                Sub = GetS("Sub"),
                FirstName = GetS("FirstName"),
                LastName = GetS("LastName"),
                Email = GetS("Email"),
                PhoneNumber = GetS("PhoneNumber"),
                Role = GetS("Role"),
                CreatedAt = GetS("CreatedAt")
            };
        }

        private static Dictionary<string, AttributeValue> ToItem(UserProfile p, string pk)
        {
            return new Dictionary<string, AttributeValue>
            {
                [PkName] = new AttributeValue(pk),
                ["Sub"] = new AttributeValue(p.Sub),
                ["FirstName"] = new AttributeValue(p.FirstName ?? string.Empty),
                ["LastName"] = new AttributeValue(p.LastName ?? string.Empty),
                ["Email"] = new AttributeValue(p.Email ?? string.Empty),
                ["PhoneNumber"] = new AttributeValue(p.PhoneNumber ?? string.Empty),
                ["Role"] = new AttributeValue(p.Role ?? string.Empty),
                ["CreatedAt"] = new AttributeValue(p.CreatedAt ?? string.Empty)
            };
        }
    }
}
