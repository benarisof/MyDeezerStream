using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyDeezerStream.Application.Interfaces;

namespace MyDeezerStream.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Optionnel : on peut mettre [AllowAnonymous] si on veut que la recherche soit publique
public class SearchController : ControllerBase
{
    private readonly ISearchItem _searchService;

    public SearchController(ISearchItem searchService)
    {
        _searchService = searchService;
    }

    [HttpGet("suggest")]
    public async Task<IActionResult> Suggest([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Query parameter is required.");
        }

        try
        {
            var results = await _searchService.SearchAsync(query);
            return Ok(results);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during search: {ex.Message}");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }
}