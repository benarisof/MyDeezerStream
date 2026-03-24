import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { StatsService } from '../../core/services/stats';
import { LoadingService } from '../../core/services/loading.service';
import { Stream } from '../../core/models/stats.model';

@Component({
  selector: 'app-last-streams',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './last-streams.component.html',
  styleUrls: ['./last-streams.component.scss']
})
export class LastStreamsComponent implements OnInit {
  private statsService = inject(StatsService);
  private loadingService = inject(LoadingService);

  streams = signal<Stream[]>([]);
  loading = signal(false);

  ngOnInit(): void {
    this.loadRecentStreams();
  }

  loadRecentStreams() {
    this.loading.set(true);
    this.loadingService.show();
    
    this.statsService.getRecentStreams(50).subscribe({
      next: (data) => {
        this.streams.set(data);
        this.loading.set(false);
        this.loadingService.hide();
      },
      error: () => {
        this.loading.set(false);
        this.loadingService.hide();
      }
    });
  }

  // Logique d'affichage des dates relatives
  isToday(dateInput: Date | string): boolean {
    const d = new Date(dateInput);
    const today = new Date();
    return d.toDateString() === today.toDateString();
  }

  isYesterday(dateInput: Date | string): boolean {
    const d = new Date(dateInput);
    const yesterday = new Date();
    yesterday.setDate(yesterday.getDate() - 1);
    return d.toDateString() === yesterday.toDateString();
  }

  // Formatage du temps d'écoute (ex: 203s -> 3:23)
  formatDuration(seconds?: number): string {
    if (!seconds) return '';
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }

  getColor(index: number): string {
    const colors = ['#00ef5a', '#4d79ff', '#ffcc4d', '#ff4d4d', '#b84dff', '#ff884d'];
    return colors[index % colors.length];
  }
}