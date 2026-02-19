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
    /// <summary>
    /// DynamoDB implementation of role assignment list (email → Veterinarian/Receptionist/Admin).
    /// Pk = "EMAIL#&lt;email&gt;" for lookup by email. Managed by admin/developers.
    /// </summary>
    public sealed class RoleAssignmentRepository : IRoleAssignmentRepository
    {
        private const string PkName = "Pk";
        private const string PkPrefix = "EMAIL#";

        private readonly IAmazonDynamoDB _client;
        private readonly string _tableName;

        public RoleAssignmentRepository(IAmazonDynamoDB client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _tableName = Env.RoleAssignmentsTable;
        }

        public async Task<string?> GetRoleByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var pk = PkPrefix + email.Trim().ToLowerInvariant();
            var req = new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue> { [PkName] = new AttributeValue(pk) }
            };
            var res = await _client.GetItemAsync(req).ConfigureAwait(false);
            if (res.Item == null || res.Item.Count == 0) return null;
            return res.Item.TryGetValue("AssignedRole", out var role) ? role.S : null;
        }

        public async Task<bool> CreateRoleAssignmentAsync(string email, string role)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(role)) return false;
            var pk = PkPrefix + email.Trim().ToLowerInvariant();
            var now = DateTime.UtcNow.ToString("O");
            var req = new PutItemRequest
            {
                TableName = _tableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    [PkName] = new AttributeValue(pk),
                    ["Email"] = new AttributeValue(email.Trim()),
                    ["AssignedRole"] = new AttributeValue(role),
                    ["CreatedBy"] = new AttributeValue("system"),
                    ["CreatedAt"] = new AttributeValue(now)
                }
            };
            await _client.PutItemAsync(req).ConfigureAwait(false);
            return true;
        }

        public async Task<List<RoleAssignment>> GetAllAsync()
        {
            var req = new ScanRequest { TableName = _tableName };
            var res = await _client.ScanAsync(req).ConfigureAwait(false);
            var list = new List<RoleAssignment>();
            foreach (var item in res.Items ?? Enumerable.Empty<Dictionary<string, AttributeValue>>())
                list.Add(FromItem(item));
            return list;
        }

        private static RoleAssignment FromItem(Dictionary<string, AttributeValue> item)
        {
            string GetS(string key) => item.TryGetValue(key, out var a) ? a.S ?? string.Empty : string.Empty;
            return new RoleAssignment
            {
                Pk = GetS(PkName),
                Email = GetS("Email"),
                AssignedRole = GetS("AssignedRole"),
                CreatedBy = GetS("CreatedBy"),
                CreatedAt = GetS("CreatedAt")
            };
        }
    }
}
