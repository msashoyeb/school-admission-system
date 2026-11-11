export type HoldStatus = 'HELD' | 'CONFIRMED' | 'EXPIRED' | 'CANCELLED';

export interface CreateClassInput {
  name: string;
  totalSeats: number;
}

export interface CreatedClass {
  id: string;
  name: string;
  totalSeats: number;
}

export interface ClassSummary {
  id: string;
  name: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
}

export interface SeatHold {
  holdId: string;
  classId: string;
  studentId: string;
  status: HoldStatus;
  expiresAt: string;
}

export interface HoldDetail {
  holdId: string;
  classId: string;
  status: HoldStatus;
  expiresAt: string;
}

export interface ConfirmHoldResult {
  holdId: string;
  status: HoldStatus;
}

export interface MyHold {
  holdId: string;
  classId: string;
  className: string;
  status: HoldStatus;
  expiresAt: string;
}

declare global {
  interface Window {
    __ENV__?: {
      API_BASE_URL?: string;
    };
  }
}
