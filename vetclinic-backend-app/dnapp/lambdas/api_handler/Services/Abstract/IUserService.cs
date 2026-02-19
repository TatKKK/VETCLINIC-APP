using Function.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Function.Services.Abstract
{
    public interface IUserService
    {
        Task<UserInfo?> GetUserInfoAsync(string sub);
        Task<string> DetermineUserRoleAsync(string email);
    }
}
