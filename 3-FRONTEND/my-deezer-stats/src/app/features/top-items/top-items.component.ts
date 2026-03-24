import { Component, inject, signal, effect, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { StatsService } from '../../core/services/stats';
import { PeriodService, PeriodOption } from '../../core/services/periode.service';
import { LoadingService } from '../../core/services/loading.service';
import { TopItemCardComponent } from '../../shared/components/top-item-card/top-item-card.component';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

type ItemType = 'artist' | 'album' | 'track';

@Component({
  selector: 'app-top-items',
  standalone: true,
  imports: [CommonModule, TopItemCardComponent, MatProgressSpinner],
  template: `
    <div class="page-container">
      <h1 class="page-title">Top 50 {{ titlePluriel }}</h1>

      @if (loading()) {
        <div class="spinner-container">
          <mat-spinner diameter="50"></mat-spinner>
        </div>
      } @else {
        <div class="items-grid">
          @for (item of items(); track item.id || $index; let i = $index) {
            <app-top-item-card
              [type]="type()"
              [item]="item"
              [rank]="i + 1"
              [color]="getColor(i)">
            </app-top-item-card>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .page-container { padding: 2rem; }
    .page-title { margin-bottom: 2rem; font-size: 2rem; font-weight: 500; }
    .items-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 1.5rem;
    }
    .spinner-container { display: flex; justify-content: center; margin-top: 4rem; }
  `]
})
export class TopItemsComponent {
  private route = inject(ActivatedRoute);
  private statsService = inject(StatsService);
  private periodService = inject(PeriodService);
  private loadingService = inject(LoadingService);

  // --- SIGNALS DE DONNÉES ---
  
  private routeData = toSignal(this.route.data);

  type = computed<ItemType>(() => this.routeData()?.['type'] || 'artist');
  
  items = signal<any[]>([]);
  loading = signal(false);

  // --- LOGIQUE RÉACTIVE ---

  /**
   * L'effect doit être déclaré ici (contexte d'injection).
   * Il se déclenchera dès que currentPeriod() OU type() change.
   */
  private _autoLoader = effect(() => {
    const period = this.periodService.currentPeriod();
    const currentType = this.type(); 
    
    this.loadItems(period, currentType);
  });

  // --- MÉTHODES ---

  get titlePluriel(): string {
    const t = this.type();
    return t === 'artist' ? 'Artistes' : t === 'album' ? 'Albums' : 'Titres';
  }

  private loadItems(period: PeriodOption, type: ItemType) {
    this.loadingService.show();

    let request$;
    switch (type) {
      case 'artist':
        request$ = this.statsService.getTopArtists(50, period);
        break;
      case 'album':
        request$ = this.statsService.getTopAlbums(50, period);
        break;
      case 'track':
        request$ = this.statsService.getTopTracks(50, period);
        break;
    }

    request$.subscribe({
      next: (data) => {
        this.items.set(data);
        this.loadingService.hide();
      },
      error: () => {
        this.loadingService.hide();
      }
    });
  }

  getColor(index: number): string {
    const colors = ['#00ef5a', '#4d79ff', '#ffcc4d', '#ff4d4d', '#b84dff', '#ff884d'];
    return colors[index % colors.length];
  }
}