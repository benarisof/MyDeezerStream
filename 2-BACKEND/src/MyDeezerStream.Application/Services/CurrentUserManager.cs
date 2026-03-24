using MyDeezerStream.Application.Interfaces;
using MyDeezerStream.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MyDeezerStream.Application.Services
{
    public class CurrentUserManager
    {
        private readonly IUserContext _userContext;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<CurrentUserManager> _logger;

        public CurrentUserManager(IUserContext userContext, IUserRepository userRepository, ILogger<CurrentUserManager> logger)
        {
            _userContext = userContext;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<User> GetCurrentUserAsync()
        {
            var auth0Id = _userContext.GetCurrentAuth0UserId();
            var user = await _userRepository.GetByAuth0IdAsync(auth0Id);

            if (user == null)
            {
                _logger.LogWarning("Nouvel utilisateur : Création dans la table 'user'.");
                user = new User { Auth0Id = auth0Id };
                await _userRepository.AddAsync(user);
            }

            _logger.LogInformation("Utilisateur identifié. ID SQL (userId): {Id}", user.Id);

            if (user.Id <= 0)
                throw new InvalidOperationException("L'ID récupéré de la table 'user' est invalide.");

            return user;
        }
    }
}