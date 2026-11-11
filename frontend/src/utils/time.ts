import type { HoldStatus } from '../types/admission';

export function getRemainingSeconds(expiresAt: string, nowMs: number): number {
  const expiresMs = Date.parse(expiresAt);
  if (Number.isNaN(expiresMs)) {
    return 0;
  }

  return Math.max(0, Math.ceil((expiresMs - nowMs) / 1000));
}

export function resolveEffectiveStatus(status: HoldStatus, expiresAt: string, nowMs: number): HoldStatus {
  if (status === 'HELD' && getRemainingSeconds(expiresAt, nowMs) === 0) {
    return 'EXPIRED';
  }

  return status;
}

export function formatCountdown(totalSeconds: number): string {
  const safeSeconds = Math.max(0, totalSeconds);
  const minutes = Math.floor(safeSeconds / 60);
  const seconds = safeSeconds % 60;
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
}
