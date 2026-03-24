import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class LoadingService {
  private _isLoading = signal<boolean>(false);

  readonly isLoading = this._isLoading.asReadonly();

  show(): void {
    setTimeout(() => this._isLoading.set(true), 0);
  }

  hide(): void {
    setTimeout(() => this._isLoading.set(false), 0);
  }
}