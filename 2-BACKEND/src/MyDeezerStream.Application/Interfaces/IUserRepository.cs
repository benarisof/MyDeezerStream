using MyDeezerStream.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByAuth0IdAsync(string auth0Id);
        Task AddAsync(User user);
    }
}
