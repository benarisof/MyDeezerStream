import { Component, inject, signal, effect, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subscription } from 'rxjs';

import { StatsService } from '../../core/services/stats';
import { PeriodOption, PeriodService } from '../../core/services/periode.service';
import { LoadingService } from '../../core/services/loading.service';
import { AlbumDetail } from '../../core/models/detail.model';
import { formatListeningTime } from '../../shared/helper/util';

@Component({
  selector: 'app-album-detail',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  templateUrl: './album-detail.component.html',
  styleUrls: ['./album-detail.component.scss']
})
export class AlbumDetailComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private statsService = inject(StatsService);
  private periodService = inject(PeriodService);
  private loadingService = inject(LoadingService);

  formatListeningTime = formatListeningTime;

  // Signaux
  artistName = signal<string>('');
  albumName = signal<string>('');
  detail = signal<AlbumDetail | null>(null);
  loading = signal<boolean>(false);

  private routeSub?: Subscription;

  constructor() {
    // Recharge quand la période change
    effect(() => {
      const period = this.periodService.currentPeriod();
      if (this.artistName() && this.albumName()) {
        this.loadDetails(period);
      }
    });
  }

  ngOnInit(): void {
    this.routeSub = this.route.params.subscribe(params => {
      // On suppose une route du type : '/artist/:artistName/album/:albumName'
      const artist = params['artistName'] ? decodeURIComponent(params['artistName']) : '';
      const album = params['albumName'] ? decodeURIComponent(params['albumName']) : '';
      this.artistName.set(artist);
      this.albumName.set(album);
    });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  private loadDetails(period: PeriodOption): void {
    const artist = this.artistName();
    const album = this.albumName();
    if (!artist || !album) return;

    this.loading.set(true);
    this.loadingService.show();

    this.statsService.getAlbumDetails(artist, album, period).subscribe({
      next: (data: AlbumDetail) => {
        // Tri des pistes par nombre d'écoutes (décroissant)
        if (data?.trackDtos) {
          data.trackDtos.sort((a, b) => (b.count ?? 0) - (a.count ?? 0));
        }
        this.detail.set(data);
        this.loading.set(false);
        this.loadingService.hide();
      },
      error: () => {
        this.detail.set(null);
        this.loading.set(false);
        this.loadingService.hide();
      }
    });
  }
}