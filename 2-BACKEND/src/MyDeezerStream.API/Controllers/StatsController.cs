using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyDeezerStream.Application.DTOs;
using MyDeezerStream.Application.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly IStreamStatisticsService _statisticsService;
    private readonly ISearchItem _searchItem;

    public StatsController(IStreamStatisticsService statisticsService, ISearchItem searchItem)
    {
        _statisticsService = statisticsService;
        _searchItem = searchItem;
    }

    [Authorize]
    [HttpGet("top-artists")]
    public async Task<IActionResult> GetTopArtists(
        [FromQuery] int limit = 10,
        [FromQuery] int days = -1,
        [FromQuery] string? range = null)
    {
        var result = await _statisticsService.GetTopArtistsAsync(limit, days, range);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("top-tracks")]
    public async Task<IActionResult> GetTopTracks(
        [FromQuery] int limit = 10,
        [FromQuery] int days = -1,
        [FromQuery] string? range = null)
    {
        var result = await _statisticsService.GetTopTracksAsync(limit, days, range);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("top-albums")]
    public async Task<IActionResult> GetTopAlbums(
        [FromQuery] int limit = 10,
        [FromQuery] int days = -1,
        [FromQuery] string? range = null)
    {
        var result = await _statisticsService.GetTopAlbumsAsync(limit, days, range);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("recent")]
    public async Task<IActionResult> GetLastStream([FromQuery] int limit = 10)
    {
        var result = await _statisticsService.GetLastStreamAsync(limit);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("artist/{artistName}")]
    public async Task<IActionResult> GetArtistDetails(
        string artistName,
        [FromQuery] int days = -1,
        [FromQuery] string? range = null)
    {
        ArtistDto result = await _searchItem.GetArtistDetailsAsync(artistName, days, range);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("album/{albumName}/{artistName}")]
    public async Task<IActionResult> GetAlbumDetails(
        string albumName,
        string artistName,
        [FromQuery] int days = -1,
        [FromQuery] string? range = null)
    {
        AlbumDto result = await _searchItem.GetAlbumDetailsAsync(albumName, artistName, days, range);
        return Ok(result);
    }
}