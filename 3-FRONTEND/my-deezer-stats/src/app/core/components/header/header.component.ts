// header.component.ts
import { Component, inject, ViewChild, ElementRef, OnDestroy, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subscription, Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';

import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';

import { AuthService } from '@auth0/auth0-angular';
import { toSignal } from '@angular/core/rxjs-interop';

import { ImportService, ImportResponse } from '../../services/import.service';
import { PeriodService, PeriodOption } from '../../services/periode.service';
import { SearchService } from '../../services/search.service';
import { SearchSuggestion } from '../../models/search-suggestion.model';
import { StatsService } from '../../services/stats';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent implements OnDestroy, AfterViewInit {
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
  @ViewChild('searchContainer') searchContainer!: ElementRef<HTMLDivElement>;

  private auth = inject(AuthService);
  private importService = inject(ImportService);
  private snackBar = inject(MatSnackBar);
  private router = inject(Router);
  protected periodService = inject(PeriodService);
  private searchService = inject(SearchService);
  private statsService = inject(StatsService);

  isAuthenticated = toSignal(this.auth.isAuthenticated$, { initialValue: false });
  user = toSignal(this.auth.user$, { initialValue: null });

  searchQuery: string = '';
  suggestions: SearchSuggestion[] = [];
  suggestionsVisible = false;

  private searchSubject = new Subject<string>();
  private searchSub: Subscription;

  constructor() {
    // Debounce + switchMap pour suggestions
    this.searchSub = this.searchSubject.pipe(
      debounceTime(250),
      distinctUntilChanged(),
      switchMap(q => this.searchService.suggest(q))
    ).subscribe(results => {
      this.suggestions = results;
      this.suggestionsVisible = results.length > 0;
    });
  }

  ngAfterViewInit(): void {
    // Fermer suggestions au clic hors du conteneur
    document.addEventListener('click', this.onClickOutside.bind(this));
  }

  ngOnDestroy(): void {
    document.removeEventListener('click', this.onClickOutside.bind(this));
    this.searchSub?.unsubscribe();
  }

  onClickOutside(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!this.searchContainer.nativeElement.contains(target)) {
      this.closeSuggestions();
    }
  }

  onSearchInputChange(): void {
    this.searchSubject.next(this.searchQuery);
    if (!this.searchQuery.trim()) {
      this.closeSuggestions();
    }
  }

  onSearchInputFocus(): void {
    if (this.suggestions.length > 0) this.suggestionsVisible = true;
  }

  onSearch(): void {
    const q = this.searchQuery?.trim();
    if (!q) return;
    this.router.navigate(['/search'], { queryParams: { q } });
    this.closeSuggestions();
  }

  onSelectSuggestion(s: SearchSuggestion): void {
    if (!s) return;

    // Précharger les détails avant navigation
    if (s.type === 'artist') {
    if (!s.name || s.name.trim() === '') {
      console.warn('❌ Nom d\'artiste manquant');
      return;
    }
    const encoded = encodeURIComponent(s.name);
    this.statsService.getArtistDetails(s.name).subscribe({
      next: res => console.log('Artist details loaded', res),
      error: err => console.error('Erreur chargement artiste', err)
    });
    this.router.navigate(['/artist', encoded]);

    } else if (s.type === 'album') {
      const artist = s.artist ?? '';
      const album = s.name;
      const artistEnc = encodeURIComponent(artist);
      const albumEnc = encodeURIComponent(album);

      this.statsService.getAlbumDetails(artist, album).subscribe({
        next: res => console.log('Album details loaded', res),
        error: err => console.error('Erreur chargement album', err)
      });
      this.router.navigate(['/artist', artistEnc, 'album', albumEnc]);
    }

    this.closeSuggestions();
    this.searchQuery = '';
  }

  clearSearch(): void {
    this.searchQuery = '';
    this.closeSuggestions();
  }

  closeSuggestions(): void {
    this.suggestionsVisible = false;
  }

  // Période, login/logout, import Excel (inchangé)
  onPeriodChange(period: PeriodOption): void { this.periodService.setPeriod(period); }
  login(): void { this.auth.loginWithRedirect().subscribe({ error: err => console.error(err) }); }
  logout(): void { this.auth.logout({ logoutParams: { returnTo: window.location.origin } }); }
  importExcel(): void { this.fileInput.nativeElement.click(); }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    const maxSize = 10 * 1024 * 1024;

    if (file.size > maxSize) {
      this.showSnackBar('Le fichier est trop volumineux (max 10 Mo)', 'error-snackbar');
      input.value = '';
      return;
    }

    const allowedTypes = ['application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'];
    if (!allowedTypes.includes(file.type)) {
      this.showSnackBar('Veuillez sélectionner un fichier Excel valide (.xlsx)', 'error-snackbar');
      return;
    }

    this.importService.uploadExcel(file).subscribe({
      next: (response: ImportResponse) => this.showSnackBar(`Import réussi : ${response.importedCount} éléments importés`, 'success-snackbar'),
      error: (err: any) => this.showSnackBar('Erreur lors de l\'import', 'error-snackbar', 7000)
    });

    input.value = '';
  }

  private showSnackBar(message: string, panelClass: string, duration: number = 5000): void {
    this.snackBar.open(message, 'Fermer', { duration, panelClass: [panelClass] });
  }
}