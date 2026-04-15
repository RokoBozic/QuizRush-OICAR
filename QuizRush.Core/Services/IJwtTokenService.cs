using QuizRush.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizRush.Core.Services
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user);
    }
}
