// search-suggestion.model.ts
export interface SearchSuggestion {
  type: 'artist' | 'album' | 'track' | string;
  name: string;
  artist?: string; // pour album/track
  album?: string;  // pour track (ou album suggestion)
  id?: string;
}