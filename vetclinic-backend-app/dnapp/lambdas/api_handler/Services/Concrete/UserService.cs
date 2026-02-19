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
        private readonly IUserProfileRepository _userProfileRepository;

        public UserService(IRoleAssignmentRepository roleAssignmentRepository, IUserProfileRepository userProfileRepository)
        {
            _roleAssignmentRepository = roleAssignmentRepository ?? throw new ArgumentNullException(nameof(roleAssignmentRepository));
            _userProfileRepository = userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
        }

        public async Task<UserInfo?> GetUserInfoAsync(string sub)
        {
            if (string.IsNullOrWhiteSpace(sub)) return null;
            var profile = await _userProfileRepository.GetBySubAsync(sub).ConfigureAwait(false);
            if (profile == null) return null;
            return new UserInfo { Sub = profile.Sub, Email = profile.Email, Role = profile.Role };
        }

        public async Task<string> DetermineUserRoleAsync(string email)
        {
            // Check if email exists in role assignment table
            var assignedRole = await _roleAssignmentRepository.GetRoleByEmailAsync(email);

            // If found, return the assigned role; otherwise, default to Client
            return assignedRole ?? UserRoles.Client;
        }
    }

}
