import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';
import { CommonModule } from '@angular/common';
import { LoadingOverlayComponent } from './shared/components/loading/loading-overlay.component';
import { HeaderComponent } from './core/components/header/header.component';
@Component({
  selector: 'ds-root',
  imports: [RouterOutlet, CommonModule, LoadingOverlayComponent, HeaderComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('MyDeezerStream');
  protected readonly window = window;
  protected auth = inject(AuthService);
}
