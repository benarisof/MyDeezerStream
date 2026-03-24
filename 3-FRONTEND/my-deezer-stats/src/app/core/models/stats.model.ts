export interface TopArtist {
  artist: string;
  count: number;
  coverUrl?: string; 
}

export interface TopAlbum {
  album: string;
  artist: string;
  count: number;
  coverUrl?: string; 
}

export interface TopTrack {
  track: string;
  artist: string;
  count: number;
  coverUrl?: string; 
}

export interface Stream {
  songTitle: string; 
  artist: string;
  albumTitle?: string;
  listeningTime?: number;
  date: Date; 
}


