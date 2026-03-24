using Microsoft.EntityFrameworkCore;
using MyDeezerStream.Application.Interfaces;
using MyDeezerStream.Domain.Entities;
using MyDeezerStream.Infrastructure.Data;

namespace MyDeezerStream.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context) => _context = context;

        public async Task<User?> GetByAuth0IdAsync(string auth0Id)
        {
            // On cherche sur la colonne userName qui semble servir d'identifiant Auth0 dans ton SQL
            return await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            // Grâce à UseIdentityByDefaultColumn, user.Id contient maintenant le UserId du SQL
        }
    }
}