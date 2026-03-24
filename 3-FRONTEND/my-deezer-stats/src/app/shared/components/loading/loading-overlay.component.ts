// loading-overlay.component.ts
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LoadingService } from '../../../core/services/loading.service';

@Component({
  selector: 'app-loading-overlay',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  template: `
    @if (loadingService.isLoading()) { <div class="overlay">
        <div class="spinner-container">
          <mat-spinner diameter="60"></mat-spinner>
          <p>Traitement en cours...</p>
        </div>
      </div>
    }
  `,
  styles: [`
    .overlay {
      position: fixed;
      top: 0; left: 0;
      width: 100%; height: 100%;
      background-color: rgba(0, 0, 0, 0.7);
      z-index: 9999;
      display: flex;
      justify-content: center;
      align-items: center;
      backdrop-filter: blur(4px);
    }
    .spinner-container { text-align: center; color: white; font-weight: bold; }
  `]
})
export class LoadingOverlayComponent {
  loadingService = inject(LoadingService);
}