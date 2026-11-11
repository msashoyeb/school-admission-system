import type {ClassSummary, ConfirmHoldResult, CreateClassInput, CreatedClass, HoldDetail, MyHold, SeatHold,} from '../types/admission';

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

export function getApiBaseUrl(): string {
  const runtimeUrl = typeof window !== 'undefined' ? window.__ENV__?.API_BASE_URL : undefined;
  const rawUrl = runtimeUrl || import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080';
  return rawUrl.replace(/\/+$/, '');
}

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${getApiBaseUrl()}${path}`, init);
  } catch {
    throw new ApiError(0, 'Cannot reach the admission server. Please check that the backend is running.');
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  let parsed: unknown = null;
  if (text) {
    try {
      parsed = JSON.parse(text);
    } catch {
      parsed = null;
    }
  }

  if (!response.ok) {
    const serverMessage =
      parsed && typeof parsed === 'object' && 'error' in parsed && typeof parsed.error === 'string'
        ? parsed.error
        : `Request failed with status ${response.status}.`;
    throw new ApiError(response.status, serverMessage);
  }

  return parsed as T;
}

export function createClass(input: CreateClassInput): Promise<CreatedClass> {
  return requestJson<CreatedClass>('/classes', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  });
}

export function getClasses(): Promise<ClassSummary[]> {
  return requestJson<ClassSummary[]>('/classes');
}

export function getClassById(classId: string): Promise<ClassSummary> {
  return requestJson<ClassSummary>(`/classes/${encodeURIComponent(classId)}`);
}

export function createHold(classId: string, studentId: string): Promise<SeatHold> {
  return requestJson<SeatHold>(`/classes/${encodeURIComponent(classId)}/holds`, {
    method: 'POST',
    headers: { 'X-Student-Id': studentId },
  });
}

export function getHoldById(holdId: string): Promise<HoldDetail> {
  return requestJson<HoldDetail>(`/holds/${encodeURIComponent(holdId)}`);
}

export function confirmHold(holdId: string, studentId: string): Promise<ConfirmHoldResult> {
  return requestJson<ConfirmHoldResult>(`/holds/${encodeURIComponent(holdId)}/confirm`, {
    method: 'POST',
    headers: { 'X-Student-Id': studentId },
  });
}

export function cancelHold(holdId: string, studentId: string): Promise<void> {
  return requestJson<void>(`/holds/${encodeURIComponent(holdId)}`, {
    method: 'DELETE',
    headers: { 'X-Student-Id': studentId },
  });
}

export function getMyHolds(studentId: string): Promise<MyHold[]> {
  return requestJson<MyHold[]>('/me/holds', {
    headers: { 'X-Student-Id': studentId },
  });
}
