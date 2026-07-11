using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryApp.Application.Common
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(Guid userId, string userName, IList<string> roles);
    }
}
