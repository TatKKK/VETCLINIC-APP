using Function.Models;
using Function.Repositories.Abstract;
using Function.Services.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Function.Services.Concrete
{
    public class UserService : IUserService
    {
        private readonly IRoleAssignmentRepository _roleAssignmentRepository;

        public async Task<string> DetermineUserRoleAsync(string email)
        {
            // Check if email exists in role assignment table
            var assignedRole = await _roleAssignmentRepository.GetRoleByEmailAsync(email);

            // If found, return the assigned role; otherwise, default to Client
            return assignedRole ?? UserRoles.Client;
        }
    }

}
