using Microsoft.Extensions.Logging;
using MyDeezerStream.Application.DTOs;
using MyDeezerStream.Application.Interfaces;
using MyDeezerStream.Domain.Entities;
using MyDeezerStream.Domain.Entities.Stats;
using MyDeezerStream.Domain.Interfaces;
using OfficeOpenXml;
using StreamEntity = MyDeezerStream.Domain.Entities.Stats.Stream;

namespace MyDeezerStream.Application.Services;

public class ExcelImportService : IExcelImportService
{
    private readonly IArtistRepository _artistRepository;
    private readonly IAlbumRepository _albumRepository;
    private readonly ITrackRepository _trackRepository;
    private readonly IStreamRepository _streamRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExcelImportService> _logger;

    public ExcelImportService(
        IArtistRepository artistRepository,
        IAlbumRepository albumRepository,
        ITrackRepository trackRepository,
        IStreamRepository streamRepository,
        IUnitOfWork unitOfWork,
        ILogger<ExcelImportService> logger)
    {
        _artistRepository = artistRepository;
        _albumRepository = albumRepository;
        _trackRepository = trackRepository;
        _streamRepository = streamRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> ImportFromExcelAsync(System.IO.Stream fileStream, int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Début de l'import Excel pour UserId: {UserId}", userId);

        if (userId <= 0)
            throw new ArgumentException("L'ID utilisateur est requis et doit être supérieur à 0.");

        var rawStreams = ReadExcelSheet(fileStream);
        if (!rawStreams.Any()) return 0;

        int importedCount = 0;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Étape 1 : Préparation des entités (Artistes, Albums, Tracks)
            var streamsToInsert = await PrepareStreamsAsync(rawStreams, userId, cancellationToken);

            // Étape 2 : Bulk Insert des streams liés au UserId
            _logger.LogInformation("Insertion de {Count} streams pour l'utilisateur {UserId}...", streamsToInsert.Count, userId);
            await _streamRepository.BulkInsertStreamsAsync(streamsToInsert, cancellationToken);

            importedCount = streamsToInsert.Count;
        }, cancellationToken);

        return importedCount;
    }

    private List<RawStreamDto> ReadExcelSheet(System.IO.Stream fileStream)
    {
        using var package = new ExcelPackage(fileStream);
        // 9ème feuille (index 8)
        var worksheet = package.Workbook.Worksheets[8];
        if (worksheet == null)
            throw new InvalidOperationException("La 9ème feuille est introuvable.");

        int rowCount = worksheet.Dimension.Rows;
        var result = new List<RawStreamDto>();

        for (int row = 2; row <= rowCount; row++)
        {
            // --- EXTRACTION DU LISTENING TIME (COLONNE 6) ---
            var listeningValue = worksheet.Cells[row, 6].Value;
            int? listeningTime = null;

            if (listeningValue != null && double.TryParse(listeningValue.ToString(), out double dblVal))
            {
                int intVal = (int)Math.Round(dblVal);
                // On ignore le -1 visible sur ton image pour avoir du NULL en base
                listeningTime = (intVal >= 0) ? intVal : null;
            }

            var raw = new RawStreamDto
            {
                // Colonne 1 : Song Title
                SongTitle = worksheet.Cells[row, 1].Text?.Trim() ?? string.Empty,
                // Colonne 2 : Artist
                Artist = worksheet.Cells[row, 2].Text?.Trim() ?? string.Empty,
                // Colonne 4 : Album Title
                AlbumTitle = worksheet.Cells[row, 4].Text?.Trim() ?? string.Empty,
                // Colonne 6 : Listening Time
                ListeningTime = listeningTime,
                // Colonne 9 : Date
                Date = ParseDateTime(worksheet.Cells[row, 9].Text)
            };

            if (string.IsNullOrWhiteSpace(raw.SongTitle) || string.IsNullOrWhiteSpace(raw.Artist))
                continue;

            result.Add(raw);
        }

        return result;
    }

    private int? ParseNullableInt(string input)
    {
        if (int.TryParse(input, out int value))
            return value == -1 ? null : value;
        return null;
    }

    private DateTime ParseDateTime(string input)
    {
        if (DateTime.TryParse(input, out DateTime dt))
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return DateTime.UtcNow;
    }

    private async Task<List<StreamEntity>> PrepareStreamsAsync(List<RawStreamDto> rawStreams, int userId, CancellationToken cancellationToken)
    {
        // --- 1. Artistes ---
        var allArtistNames = rawStreams
            .SelectMany(r => SplitArtists(r.Artist))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingArtists = await _artistRepository.GetExistingArtistsAsync(allArtistNames, cancellationToken);

        var newArtists = allArtistNames
            .Where(name => !existingArtists.ContainsKey(name))
            .Select(name => new Artist { ArtistName = name })
            .ToList();

        if (newArtists.Any())
        {
            await _artistRepository.AddArtistsAsync(newArtists, cancellationToken);
            foreach (var artist in newArtists)
                existingArtists[artist.ArtistName] = artist.ArtistId;
        }

        // --- 2. Albums ---
        var allAlbumNames = rawStreams
            .Select(r => r.AlbumTitle)
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingAlbums = await _albumRepository.GetExistingAlbumsAsync(allAlbumNames, cancellationToken);

        var newAlbums = allAlbumNames
            .Where(name => !existingAlbums.ContainsKey(name))
            .Select(name => new Album { AlbumName = name })
            .ToList();

        if (newAlbums.Any())
        {
            await _albumRepository.AddAlbumsAsync(newAlbums, cancellationToken);
            foreach (var album in newAlbums)
                existingAlbums[album.AlbumName] = album.AlbumId;
        }

        // --- 3. Pistes ---
        var allTrackNames = rawStreams.Select(r => r.SongTitle).Distinct().ToList();
        var existingTracksList = await _trackRepository.GetExistingTracksAsync(allTrackNames, cancellationToken);
        var trackDict = existingTracksList.ToDictionary(
            t => (t.TrackName, t.AlbumId),
            t => t
        );

        var tracksToCreate = new List<Track>();

        foreach (var raw in rawStreams)
        {
            int? albumId = string.IsNullOrWhiteSpace(raw.AlbumTitle) ? null : existingAlbums[raw.AlbumTitle];
            var key = (raw.SongTitle, albumId);

            if (trackDict.ContainsKey(key) || tracksToCreate.Any(t => t.TrackName == raw.SongTitle && t.AlbumId == albumId))
                continue;

            var artistNames = SplitArtists(raw.Artist);
            var newTrack = new Track
            {
                TrackName = raw.SongTitle,
                AlbumId = albumId,
                TrackArtists = artistNames.Select(name => new TrackArtist
                {
                    ArtistId = existingArtists[name]
                }).ToList()
            };
            tracksToCreate.Add(newTrack);
        }

        if (tracksToCreate.Any())
        {
            await _trackRepository.AddTracksAsync(tracksToCreate, cancellationToken);
            foreach (var track in tracksToCreate)
            {
                trackDict[(track.TrackName, track.AlbumId)] = track;
            }
        }

        // --- 4. Streams ---
        var streams = new List<StreamEntity>();
        foreach (var raw in rawStreams)
        {
            int? albumId = string.IsNullOrWhiteSpace(raw.AlbumTitle) ? null : existingAlbums[raw.AlbumTitle];
            var key = (raw.SongTitle, albumId);
            if (!trackDict.TryGetValue(key, out var track))
            {
                _logger.LogWarning("Piste introuvable pour {SongTitle} (album {AlbumId}), ligne ignorée", raw.SongTitle, albumId);
                continue;
            }

            streams.Add(new StreamEntity
            {
                UserId = userId,
                TrackId = track.TrackId,
                PlayedAt = raw.Date,
                ListeningTime = raw.ListeningTime
            });
        }

        return streams;
    }

    private List<string> SplitArtists(string artistField)
    {
        if (string.IsNullOrWhiteSpace(artistField))
            return new List<string>();
        return artistField.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }
}