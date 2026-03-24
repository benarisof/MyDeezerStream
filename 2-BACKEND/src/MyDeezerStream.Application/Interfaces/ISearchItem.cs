using MyDeezerStream.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Application.Interfaces
{
    public interface ISearchItem
    {
        Task<AlbumDto> GetAlbumDetailsAsync(string albumName, string artistName, int days);
        Task<ArtistDto> GetArtistDetailsAsync(string artistName, int days);
        Task<List<SearchSuggestionDto>> SearchAsync(string query);
    }
}
