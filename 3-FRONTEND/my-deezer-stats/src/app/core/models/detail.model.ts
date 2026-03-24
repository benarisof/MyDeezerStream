export interface TrackDetail {
  name: string;
  album: string;
  listeningTime: number;
  count: number;
}

export interface ArtistDetail {
  name: string;
  count: number;           
  listeningTime: number;  
  coverUrl: string;
  trackDtos: TrackDetail[];
}

export interface AlbumDetail {
  name: string;
  artist: string;
  coverUrl: string;
  count: number;       
  listeningTime: number;   
  trackDtos: TrackDetail[];
}