import { Injectable, signal } from '@angular/core';

export type PeriodOption = 7 | 30 | 90 | 180 | 365 | "all";

@Injectable({
  providedIn: 'root'
})
export class PeriodService {
  private currentPeriodSignal = signal<PeriodOption>(30); 
  readonly currentPeriod = this.currentPeriodSignal.asReadonly();

  setPeriod(period: PeriodOption): void {
    this.currentPeriodSignal.set(period);
  }
}