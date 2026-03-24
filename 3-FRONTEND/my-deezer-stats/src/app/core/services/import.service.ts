import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent, HttpEventType } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap, finalize } from 'rxjs/operators';
import { LoadingService } from './loading.service';

export interface ImportResponse {
  importedCount: number;
  message: string;
}
@Injectable({
  providedIn: 'root'
})
export class ImportService {
  private readonly apiUrl = 'http://localhost:5257/api/import'

  constructor(
    private http: HttpClient,
    private loadingService: LoadingService,
  ) {}

  /**
   * Envoie le fichier Excel à l'API .NET
   * @param file Le fichier récupéré via l'input HTML
   */
  uploadExcel(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);

    // On déclenche le spinner global
    this.loadingService.show();

    return this.http.post(`${this.apiUrl}/excel`, formData).pipe(
      tap(() => {
      }),
      finalize(() => {
        this.loadingService.hide();
      })
    );
  }

  /**
   * Optionnel : Version avancée pour suivre la progression de l'upload (0 à 100%)
   * Utile pour tes fichiers de 100 Mo sur des connexions lentes.
   */
  uploadExcelWithProgress(file: File): Observable<HttpEvent<any>> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post(`${this.apiUrl}/excel`, formData, {
      reportProgress: true,
      observe: 'events'
    });
  }
}