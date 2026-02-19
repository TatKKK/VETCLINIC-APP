using Function.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Function.Repositories.Abstract
{
    public interface IRoleAssignmentRepository
    {
        Task<string?> GetRoleByEmailAsync(string email);
        Task<bool> CreateRoleAssignmentAsync(string email, string role);
        Task<List<RoleAssignment>> GetAllAsync();
    }
}
