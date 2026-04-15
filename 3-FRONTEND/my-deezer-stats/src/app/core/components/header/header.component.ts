import { Component, inject, ViewChild, ElementRef, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subscription, Subject, finalize } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, tap } from 'rxjs/operators';

// Angular Material
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

// Auth0 & Signals
import { AuthService } from '@auth0/auth0-angular';
import { toSignal } from '@angular/core/rxjs-interop';

// Services & Modèles
import { ImportService, ImportResponse } from '../../services/import.service';
import { PeriodService, PeriodOption } from '../../services/periode.service';
import { SearchService } from '../../services/search.service';
import { SearchSuggestion } from '../../models/search-suggestion.model';

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
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent implements OnDestroy {
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
  @ViewChild('searchContainer') searchContainer!: ElementRef<HTMLDivElement>;

  // Injections
  private auth = inject(AuthService);
  private importService = inject(ImportService);
  private snackBar = inject(MatSnackBar);
  private router = inject(Router);
  private searchService = inject(SearchService);
  protected periodService = inject(PeriodService);

  // Signaux d'état
  isAuthenticated = toSignal(this.auth.isAuthenticated$, { initialValue: false });
  user = toSignal(this.auth.user$, { initialValue: null });

  // Recherche
  searchQuery: string = '';
  suggestions: SearchSuggestion[] = [];
  suggestionsVisible = false;
  isLoadingSuggestions = false;

  private searchSubject = new Subject<string>();
  private searchSub: Subscription;

  constructor() {
    // Pipeline de recherche : Debounce + SwitchMap
    this.searchSub = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      tap(q => {
        if (q.trim()) this.isLoadingSuggestions = true;
      }),
      switchMap(q => 
        this.searchService.suggest(q).pipe(
          finalize(() => this.isLoadingSuggestions = false)
        )
      )
    ).subscribe({
      next: (results) => {
        this.suggestions = results;
        this.suggestionsVisible = results.length > 0;
      },
      error: (err) => {
        console.error('Erreur suggestions:', err);
        this.isLoadingSuggestions = false;
      }
    });
  }

  /**
   * Ferme les suggestions si on clique en dehors du conteneur de recherche
   */
  @HostListener('document:click', ['$event'])
  onClickOutside(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (this.searchContainer && !this.searchContainer.nativeElement.contains(target)) {
      this.closeSuggestions();
    }
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  // --- LOGIQUE DE RECHERCHE ---

  onSearchInputChange(): void {
    const q = this.searchQuery.trim();
    if (!q) {
      this.suggestions = [];
      this.closeSuggestions();
      return;
    }
    this.searchSubject.next(q);
  }

  onSearchInputFocus(): void {
    if (this.suggestions.length > 0) {
      this.suggestionsVisible = true;
    }
  }

  onSearch(): void {
    const q = this.searchQuery?.trim();
    if (!q) return;
    
    this.router.navigate(['/search'], { queryParams: { q } });
    this.closeSuggestions();
  }

  onSelectSuggestion(s: SearchSuggestion): void {
    if (!s || !s.name) return;

    // Navigation directe (le composant de destination chargera ses propres datas)
    if (s.type === 'artist') {
      this.router.navigate(['/artist', encodeURIComponent(s.name)]);
    } else if (s.type === 'album') {
      const artistEnc = encodeURIComponent(s.artist || 'Inconnu');
      const albumEnc = encodeURIComponent(s.name);
      this.router.navigate(['/artist', artistEnc, 'album', albumEnc]);
    }

    this.clearSearch();
  }

  clearSearch(): void {
    this.searchQuery = '';
    this.closeSuggestions();
    this.suggestions = [];
  }

  closeSuggestions(): void {
    this.suggestionsVisible = false;
  }

  // --- ACTIONS HEADER ---

  onPeriodChange(period: PeriodOption): void {
    this.periodService.setPeriod(period);
  }

  login(): void {
    this.auth.loginWithRedirect().subscribe({
      error: (err) => console.error('Erreur Login:', err)
    });
  }

  logout(): void {
    this.auth.logout({ 
      logoutParams: { returnTo: window.location.origin } 
    });
  }

  // --- IMPORT EXCEL ---

  importExcel(): void {
    this.fileInput.nativeElement.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    const maxSize = 10 * 1024 * 1024; // 10 Mo

    if (file.size > maxSize) {
      this.showSnackBar('Le fichier est trop volumineux (max 10 Mo)', 'error-snackbar');
      input.value = '';
      return;
    }

    const allowedTypes = ['application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'];
    if (!allowedTypes.includes(file.type)) {
      this.showSnackBar('Veuillez sélectionner un fichier Excel valide (.xlsx)', 'error-snackbar');
      input.value = '';
      return;
    }

    this.importService.uploadExcel(file).subscribe({
      next: (response: ImportResponse) => {
        this.showSnackBar(`Import réussi : ${response.importedCount} éléments importés`, 'success-snackbar');
      },
      error: (err) => {
        console.error('Erreur import:', err);
        this.showSnackBar("Erreur lors de l'import", 'error-snackbar', 7000);
      }
    });

    input.value = '';
  }

  private showSnackBar(message: string, panelClass: string, duration: number = 5000): void {
    this.snackBar.open(message, 'Fermer', {
      duration,
      panelClass: [panelClass]
    });
  }
}