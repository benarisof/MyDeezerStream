import { Injectable, signal } from '@angular/core';

export type PeriodOption = 30 | 90 | 180 | 365 | "all" | "this_year" | "last_year";

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