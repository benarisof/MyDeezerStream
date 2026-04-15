import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { TopArtist, TopAlbum, TopTrack, Stream } from '../models/stats.model';
import { PeriodOption } from './periode.service'; 
import { AlbumDetail, ArtistDetail, TrackDetail } from '../models/detail.model';

@Injectable({ providedIn: 'root' })
export class StatsService {
  private http = inject(HttpClient);
  private readonly API_URL = 'http://localhost:5257/api/stats';

  /**
   * Helper pour construire les paramètres de requête selon la période
   */
  private buildParams(limit?: number, period?: PeriodOption): HttpParams {
    let params = new HttpParams();
    if (limit) params = params.set('limit', limit.toString());

    if (period) {
      if (typeof period === 'number') {
        // Périodes glissantes (30, 90, etc.)
        params = params.set('days', period.toString());
      } else if (period === 'all') {
        // Cas historique pour "Tout"
        params = params.set('days', '-1');
      } else {
        // Nouvelles périodes (this_year, last_year)
        params = params.set('range', period);
      }
    }
    return params;
  }

  getTopArtists(limit: number = 10, period?: PeriodOption): Observable<TopArtist[]> {
    const params = this.buildParams(limit, period);
    return this.http.get<TopArtist[]>(`${this.API_URL}/top-artists`, { params });
  }

  getTopAlbums(limit: number = 10, period?: PeriodOption): Observable<TopAlbum[]> {
    const params = this.buildParams(limit, period);
    return this.http.get<TopAlbum[]>(`${this.API_URL}/top-albums`, { params });
  }

  getTopTracks(limit: number = 10, period?: PeriodOption): Observable<TopTrack[]> {
    const params = this.buildParams(limit, period);
    return this.http.get<TopTrack[]>(`${this.API_URL}/top-tracks`, { params });
  }

  getRecentStreams(limit: number = 50): Observable<Stream[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<Stream[]>(`${this.API_URL}/recent`, { params });
  }

  getArtistDetails(artistName: string, period?: PeriodOption): Observable<ArtistDetail> {
    const params = this.buildParams(undefined, period);
    const encodedName = encodeURIComponent(artistName);

    return this.http.get<ArtistDetail>(`${this.API_URL}/artist/${encodedName}`, { params }).pipe(
      map(res => this.normalizeArtistDetail(res, artistName))
    );
  }

  getAlbumDetails(artistName: string, albumName: string, period?: PeriodOption): Observable<AlbumDetail> {
    const params = this.buildParams(undefined, period);
    const encodedArtist = encodeURIComponent(artistName);
    const encodedAlbum = encodeURIComponent(albumName);

    return this.http.get<AlbumDetail>(`${this.API_URL}/album/${encodedAlbum}/${encodedArtist}`, { params }).pipe(
      map(res => this.normalizeAlbumDetail(res, artistName, albumName))
    );
  }

  // Méthodes de normalisation privées pour garder le code propre
  private normalizeArtistDetail(res: any, artistName: string): ArtistDetail {
    if (!res) return { name: artistName, count: 0, listeningTime: 0, coverUrl: '', trackDtos: [] };
    return {
      name: res.name ?? artistName,
      count: res.count ?? 0,
      listeningTime: res.listeningTime ?? 0,
      coverUrl: res.coverUrl ?? '',
      trackDtos: (res.trackDtos || []).map((t: any) => ({
        name: t.name,
        album: t.album ?? '',
        count: t.count ?? 0,
        listeningTime: t.listeningTime ?? 0
      }))
    };
  }

  private normalizeAlbumDetail(res: any, artistName: string, albumName: string): AlbumDetail {
    if (!res) return { name: albumName, artist: artistName, coverUrl: '', count: 0, listeningTime: 0, trackDtos: [] };
    return {
      name: res.name ?? albumName,
      artist: res.artist ?? artistName,
      coverUrl: res.coverUrl ?? '',
      count: res.count ?? 0,
      listeningTime: res.listeningTime ?? 0,
      trackDtos: (res.trackDtos || []).map((t: any) => ({
        name: t.name,
        album: t.album ?? '',
        count: t.count ?? 0,
        listeningTime: t.listeningTime ?? 0
      }))
    };
  }
}