using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyDeezerStream.Application.Interfaces;
using MyDeezerStream.Application.Services; // Ajouté pour CurrentUserManager

namespace MyDeezerStream.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Activé car on a besoin de l'utilisateur
    public class ImportController : ControllerBase
    {
        private readonly IExcelImportService _importService;
        private readonly CurrentUserManager _currentUserManager; // Injecté à la place des Claims manuels
        private readonly ILogger<ImportController> _logger;

        public ImportController(
            IExcelImportService importService,
            CurrentUserManager currentUserManager,
            ILogger<ImportController> logger)
        {
            _importService = importService;
            _currentUserManager = currentUserManager;
            _logger = logger;
        }

        [HttpPost("excel")]
        [RequestSizeLimit(100_000_000)] // 100 MB max
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Fichier vide.");

            try
            {
                // Utilisation du manager centralisé pour récupérer ou créer l'utilisateur
                // Cela gère l'ID Auth0 (string) et nous retourne l'entité User avec son ID (int)
                var user = await _currentUserManager.GetCurrentUserAsync();
                _logger.LogInformation("Import lancé pour l'utilisateur avec l'ID interne : {UserId}", user.Id); 
                using var stream = file.OpenReadStream();

                // On passe l'ID interne (int) de notre base de données
                int count = await _importService.ImportFromExcelAsync(stream, user.Id);

                return Ok(new { ImportedCount = count, Message = "Import terminé." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'import pour l'utilisateur");
                return StatusCode(500, "Une erreur interne est survenue lors de l'importation.");
            }
        }
    }
}