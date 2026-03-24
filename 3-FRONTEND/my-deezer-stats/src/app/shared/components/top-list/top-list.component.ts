import { 
  Component, 
  inject, 
  Input, 
  AfterViewInit, 
  ViewChild, 
  ElementRef, 
  ChangeDetectorRef, 
  HostListener 
} from '@angular/core'; // CORRIGÉ : core et non common
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';

@Component({
  selector: 'app-top-list',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  templateUrl: './top-list.component.html',
  styleUrls: ['./top-list.component.scss']
})
export class TopListComponent implements AfterViewInit {
  @Input() type: 'artist' | 'album' | 'track' = 'artist';
  @Input() items: any[] = [];
  @Input() title: string = '';
  @Input() limit: number = 10;
  @ViewChild('carousel') carouselRef!: ElementRef<HTMLElement>;

  isAtStart = true;
  isAtEnd = false;

  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  ngAfterViewInit() {
    this.checkScroll();
    // Un petit délai pour s'assurer que le DOM est stable
    setTimeout(() => this.checkScroll(), 500);
  }

  @HostListener('window:resize')
  onResize() {
    this.checkScroll();
  }

  onScroll() {
    this.checkScroll();
  }

  checkScroll() {
    const el = this.carouselRef?.nativeElement;
    if (!el) return;

    // Seuil de 10px pour la précision
    this.isAtStart = el.scrollLeft <= 10;
    this.isAtEnd = el.scrollLeft + el.clientWidth >= el.scrollWidth - 10;
    
    this.cdr.detectChanges();
  }

  scroll(distance: number) {
    if (!this.carouselRef) return;
    this.carouselRef.nativeElement.scrollBy({ left: distance, behavior: 'smooth' });
    // On vérifie l'état après la fin de l'animation de scroll
    setTimeout(() => this.checkScroll(), 400);
  }

  // --- HELPER METHODS ---
  getIcon(): string {
    switch (this.type) {
      case 'artist': return 'person';
      case 'album': return 'album';
      case 'track': return 'music_note';
      default: return 'star';
    }
  }

  getName(item: any): string {
    if (this.type === 'artist') return item.artist;
    if (this.type === 'album') return item.album;
    return item.track;
  }

  getArtist(item: any): string | null {
    return this.type === 'artist' ? null : item.artist;
  }

  getPlayCount(item: any): number {
    return item.count;
  }

  hasCover(item: any): boolean {
    return !!item.coverUrl;
  }

  getCover(item: any): string {
    return item.coverUrl;
  }

  navigateToDetail(item: any) {
    if (this.type === 'artist') {
      this.router.navigate(['/artist', item.artist]);
    } else if (this.type === 'album') {
      this.router.navigate(['/artist', item.artist, 'album', item.album]);
    }
  }
}