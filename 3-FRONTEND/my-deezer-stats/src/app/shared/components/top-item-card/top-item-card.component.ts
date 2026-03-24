import { Component, inject, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';

@Component({
  selector: 'app-top-item-card',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './top-item-card.component.html',
  styleUrls: ['./top-item-card.component.scss']
})
export class TopItemCardComponent {

  @Input() type: 'artist' | 'album' | 'track' = 'artist';
  @Input() item: any;
  @Input() rank!: number;
    @Input() color?: string; 
  private router = inject(Router);

  get icon(): string {
    switch (this.type) {
      case 'artist': return 'person';
      case 'album': return 'album';
      case 'track': return 'music_note';
      default: return 'music_note';
    }
  }

  get itemName(): string {
    switch (this.type) {
      case 'artist': return this.item?.artist;
      case 'album': return this.item?.album;
      case 'track': return this.item?.track;
      default: return '';
    }
  }

  get subName(): string | null {
    if (this.type === 'artist') return null;
    return this.item?.artist ?? null;
  }

  navigateToDetail() {
    if (!this.item) return;

    if (this.type === 'artist') {
      this.router.navigate(['/artist', encodeURIComponent(this.item.artist)]);
    } else if (this.type === 'album') {
      this.router.navigate([
        '/album',
        encodeURIComponent(this.item.album),
        encodeURIComponent(this.item.artist)
      ]);
    }
  }
}