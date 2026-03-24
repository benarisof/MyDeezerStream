// search.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { SearchSuggestion } from '../models/search-suggestion.model';

@Injectable({ providedIn: 'root' })
export class SearchService {
  private http = inject(HttpClient);
  private readonly API_URL = 'http://localhost:5257/api/search'; // ajuste si besoin

  /**
   * Appelle GET api/search/suggest?query=...
   * Retourne SearchSuggestion[]
   */
  suggest(query: string): Observable<SearchSuggestion[]> {
  if (!query || !query.trim()) return of([]);

  const params = new HttpParams().set('query', query.trim());
  return this.http.get<any[]>(`${this.API_URL}/suggest`, { params }).pipe(
    tap(raw => console.log('🔍 Réponse brute du backend :', raw)), // <-- ajoutez ceci
    map((res: any[]) => {
      const suggestions = (res || []).map(item => {
        // Essayez de récupérer le nom depuis plusieurs champs possibles
        const name = item.name ?? item.Name ?? item.title ?? item.value ?? '';
        const artist = item.artist ?? item.Artist ?? item.artistName ?? undefined;
        const album = item.album ?? item.Album ?? undefined;
        return {
          type: item.type ?? item.Type ?? item.kind ?? 'track',
          name: name,
          artist: artist,
          album: album,
          id: item.id ?? item.Id ?? undefined
        } as SearchSuggestion;
      }).filter(s => s.name.trim() !== ''); // ← on garde seulement celles avec un nom

      console.log('✅ Suggestions après mapping :', suggestions);
      return suggestions;
    }),
    catchError(err => {
      console.error('Search suggest error', err);
      return of([]);
    })
  );
}
}