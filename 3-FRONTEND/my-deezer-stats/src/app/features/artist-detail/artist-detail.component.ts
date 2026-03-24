import { Component, inject, signal, effect, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { StatsService } from '../../core/services/stats';
import { PeriodOption, PeriodService } from '../../core/services/periode.service';
import { LoadingService } from '../../core/services/loading.service';
import { ArtistDetail, TrackDetail } from '../../core/models/detail.model';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { formatListeningTime } from '../../shared/helper/util';

@Component({
  selector: 'app-artist-detail',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  templateUrl: './artist-detail.component.html',
  styleUrls: ['./artist-detail.component.scss']
})
export class ArtistDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private statsService = inject(StatsService);
  private periodService = inject(PeriodService);
  private loadingService = inject(LoadingService);
  formatListeningTime = formatListeningTime;
  // signaux
  artistName = signal<string>('');
  detail = signal<ArtistDetail | null>(null);
  loading = signal<boolean>(false);

  private routeSub?: Subscription;

  constructor() {
    // recharge quand la période change
    effect(() => {
      const period = this.periodService.currentPeriod();
      if (this.artistName()) {
        this.loadDetails(period);
      }
    });
  }

  ngOnInit() {
  this.routeSub = this.route.params.subscribe(params => {
    const name = params['artistName'] ? decodeURIComponent(params['artistName']) : '';
    this.artistName.set(name); 

  });
}

  ngOnDestroy() {
    this.routeSub?.unsubscribe();
  }

  private loadDetails(period: PeriodOption) {
    const artist = this.artistName();
    if (!artist) return;

    this.loading.set(true);
    this.loadingService.show();

    this.statsService.getArtistDetails(artist, period).subscribe({
      next: (data: ArtistDetail) => {
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