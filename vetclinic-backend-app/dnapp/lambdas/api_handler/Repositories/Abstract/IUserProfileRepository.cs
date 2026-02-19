using Function.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Function.Repositories.Abstract
{
    public interface IUserProfileRepository
    {
        Task<UserProfile> GetBySubAsync(string sub);
        Task PutAsync(UserProfile profile);
 
    }
}
