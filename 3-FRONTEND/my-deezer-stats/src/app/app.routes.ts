import { Routes } from '@angular/router';
import { Dashboard } from './features/dashboard/dashboard';
import { TopItemsComponent } from './features/top-items/top-items.component';
import { LastStreamsComponent } from './features/last-streams/last-streams.component';
import { ArtistDetailComponent } from './features/artist-detail/artist-detail.component';
import { AlbumDetailComponent } from './features/album-detail/album-detail.component'; // 

export const routes: Routes = [
  { path: '', component: Dashboard },
  { path: 'top-artists', component: TopItemsComponent, data: { type: 'artist' } },
  { path: 'top-albums', component: TopItemsComponent, data: { type: 'album' } },
  { path: 'top-tracks', component: TopItemsComponent, data: { type: 'track' } },
  { path: 'last-streams', component: LastStreamsComponent, data: {type: 'stream'} },
  { path: 'artist/:artistName/album/:albumName', component: AlbumDetailComponent }, 
  { path: 'artist/:artistName', component: ArtistDetailComponent }, 
  { path: '**', redirectTo: '' }             
];