import { Component, OnInit, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { finalize, forkJoin, take, asapScheduler } from 'rxjs';

// Services
import { StatsService } from '../../core/services/stats'; 
import { LoadingService } from '../../core/services/loading.service';
import { PeriodService, PeriodOption } from '../../core/services/periode.service';
import { AuthService } from '@auth0/auth0-angular';

// Models
import { TopArtist, TopAlbum, TopTrack } from '../../core/models/stats.model';

// Angular Material
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';

// Components
import { TopListComponent } from '../../shared/components/top-list/top-list.component';

@Component({
  selector: 'ds-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    TopListComponent
  ],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.scss']
})
export class Dashboard implements OnInit {
  private statsService = inject(StatsService);
  private loadingService = inject(LoadingService);
  private periodService = inject(PeriodService);
  auth = inject(AuthService);

  // Authentification
  isAuthenticated = toSignal(this.auth.isAuthenticated$, { initialValue: false });
  user = toSignal(this.auth.user$, { initialValue: null });
  
  // État de chargement pilotant les skeletons
  loading = signal<boolean>(false);

  // Données
  artists = signal<TopArtist[]>([]);
  albums = signal<TopAlbum[]>([]);
  tracks = signal<TopTrack[]>([]);

  constructor() {
    // Réaction automatique au changement de période ou d'auth
    effect(() => {
      const isAuth = this.isAuthenticated();
      const period = this.periodService.currentPeriod();

      if (isAuth) {
        this.loadStats(period);
      }
    }, { allowSignalWrites: true });
  }

  ngOnInit(): void {
    // Monitoring des erreurs d'authentification
    this.auth.error$.subscribe(error => {
      if (error) console.error('Erreur Auth0:', error.message);
    });
  }

  /**
   * Charge les statistiques en parallèle
   */
  private loadStats(period: PeriodOption): void {
  this.loading.set(true);
  this.loadingService.show();

  forkJoin({
    artists: this.statsService.getTopArtists(10, period).pipe(take(1)),
    albums: this.statsService.getTopAlbums(10, period).pipe(take(1)),
    tracks: this.statsService.getTopTracks(10, period).pipe(take(1))
  })
  .pipe(
    finalize(() => {
      this.loading.set(false);
      this.loadingService.hide();
      console.log('🏁 Dashboard : Chargement terminé');
    })
  )
  .subscribe({
    next: (data) => {
      this.artists.set(data.artists);
      this.albums.set(data.albums);
      this.tracks.set(data.tracks);
    }
  });
}
  login(): void {
    this.auth.loginWithRedirect().subscribe({
      error: (err) => console.error('Erreur login:', err)
    });
  }

  logout(): void {
    this.auth.logout({
      logoutParams: { returnTo: window.location.origin }
    });
  }

  /**
   * Helper pour les couleurs de charts (si besoin dans le template)
   */
  getColor(index: number): string {
    const colors = ['#00ef5a', '#4d79ff', '#ffcc4d', '#ff4d4d', '#b84dff'];
    return colors[index % colors.length];
  }
}