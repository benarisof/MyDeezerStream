import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { TopArtist, TopAlbum, TopTrack, Stream } from '../models/stats.model';
import { PeriodOption } from './periode.service'; // Import du type partagé
import { AlbumDetail, ArtistDetail, TrackDetail } from '../models/detail.model';

@Injectable({ providedIn: 'root' })
export class StatsService {
  private http = inject(HttpClient);
  private readonly API_URL = 'http://localhost:5257/api/stats'; // À ajuster selon votre configuration

  /**
   * Récupère le top des artistes pour une période donnée
   * @param limit Nombre d'éléments à retourner (défaut: 10)
   * @param period Période choisie (7,30,90,180,365 ou 'all')
   */
  getTopArtists(limit: number = 10, period?: PeriodOption): Observable<TopArtist[]> {
    let params = new HttpParams().set('limit', limit.toString());
    if (period && period !== 'all') {
      params = params.set('days', period.toString());
    }
    return this.http.get<TopArtist[]>(`${this.API_URL}/top-artists`, { params });
  }

  /**
   * Récupère le top des albums pour une période donnée
   */
  getTopAlbums(limit: number = 10, period?: PeriodOption): Observable<TopAlbum[]> {
    let params = new HttpParams().set('limit', limit.toString());
    if (period && period !== 'all') {
      params = params.set('days', period.toString());
    }
    return this.http.get<TopAlbum[]>(`${this.API_URL}/top-albums`, { params });
  }

  /**
   * Récupère le top des titres pour une période donnée
   */
  getTopTracks(limit: number = 10, period?: PeriodOption): Observable<TopTrack[]> {
    let params = new HttpParams().set('limit', limit.toString());
    if (period && period !== 'all') {
      params = params.set('days', period.toString());
    }
    return this.http.get<TopTrack[]>(`${this.API_URL}/top-tracks`, { params });
  }

  getRecentStreams(limit: number = 50): Observable<Stream[]> {
  let params = new HttpParams().set('limit', limit.toString());
  return this.http.get<Stream[]>(`${this.API_URL}/recent`, { params });
}

  getArtistDetails(artistName: string, period?: PeriodOption): Observable<ArtistDetail> {
  let params = new HttpParams();
  if (period && period !== 'all') {
    params = params.set('days', period.toString());
  }
  const encodedName = encodeURIComponent(artistName);

  return this.http.get<ArtistDetail>(`${this.API_URL}/artist/${encodedName}`, { params }).pipe(
    map(res => {
      if (!res) {
        // valeur par défaut si l'API ne renvoie rien
        return {
          name: artistName,
          count: 0,
          listeningTime: 0,
          coverUrl: '',
          trackDtos: []
        } as ArtistDetail;
      }

      // On mappe les tracks pour correspondre à TrackDetail
      const trackDtos: TrackDetail[] = (res.trackDtos || []).map((t: any) => ({
        name: t.name,
        album: t.album ?? '',
        count: t.count ?? 0,
        listeningTime: t.listeningTime ?? 0
      }));

      return {
        name: res.name ?? artistName,
        count: res.count ?? 0,
        listeningTime: res.listeningTime ?? 0,
        coverUrl: res.coverUrl ?? '',
        trackDtos
      } as ArtistDetail;
    })
  );
}

 getAlbumDetails(artistName: string, albumName: string, period?: PeriodOption): Observable<AlbumDetail> {
    let params = new HttpParams();
    if (period && period !== 'all') {
      params = params.set('days', period.toString());
    }

    const encodedArtist = encodeURIComponent(artistName);
    const encodedAlbum = encodeURIComponent(albumName);

    return this.http.get<AlbumDetail>(`${this.API_URL}/album/${encodedAlbum}/${encodedArtist}`, { params }).pipe(
      map(res => {
        if (!res) {
          // Valeur par défaut si l'API ne renvoie rien
          return {
            name: albumName,
            artist: artistName,
            coverUrl : '',
            count: 0,
            listeningTime: 0,
            trackDtos: []
          } as AlbumDetail;
        }

        // Normalisation des pistes
        const trackDtos: TrackDetail[] = (res.trackDtos || []).map((t: any) => ({
          name: t.name,
          album: t.album ?? '',
          count: t.count ?? 0,
          listeningTime: t.listeningTime ?? 0
        }));

        return {
          name: res.name ?? albumName,
          artist: res.artist ?? artistName,
          coverUrl : res.coverUrl,
          count: res.count ?? 0,
          listeningTime: res.listeningTime ?? 0,
          trackDtos
        } as AlbumDetail;
      })
    );
  }

}