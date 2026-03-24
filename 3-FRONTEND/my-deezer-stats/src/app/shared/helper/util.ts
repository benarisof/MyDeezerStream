export function formatListeningTime(seconds: number): string {
  if (!seconds || seconds < 0) return '0 min';

  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);

  const parts: string[] = [];

  // On ajoute les heures si elles existent
  if (h > 0) {
    parts.push(`${h}h`);
  }

  // On ajoute les minutes (ou "0 min" si tout est à zéro pour éviter un vide)
  if (m > 0 || h === 0) {
    parts.push(`${m} min`);
  }

  return parts.join(' ');
}