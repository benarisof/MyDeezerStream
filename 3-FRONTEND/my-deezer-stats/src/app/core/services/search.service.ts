// search.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { SearchSuggestion } from '../models/search-suggestion.model';

@Injectable({ providedIn: 'root' })
export class SearchService {
  private http = inject(HttpClient);
  private readonly API_URL = 'http://localhost:5257/api/search';

  suggest(query: string): Observable<SearchSuggestion[]> {
    if (!query || query.trim().length < 2) return of([]);

    const params = new HttpParams().set('query', query.trim());
    return this.http.get<any[]>(`${this.API_URL}/suggest`, { params }).pipe(
      map(res => res.map(item => ({
        id: item.id,
        name: item.displayName, // Mapping direct
        artist: item.subtitle,
        type: item.type,
        coverUrl: item.coverUrl
      }))),
      catchError(err => {
        console.error('Search suggest error', err);
        return of([]);
      })
    );
  }
}