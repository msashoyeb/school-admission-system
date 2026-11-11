const STUDENT_ID_KEY = 'school_admission_student_id';
const SELECTED_HOLD_ID_KEY = 'school_admission_selected_hold_id';

export function isValidStudentId(value: string): boolean {
  return /^\d{17}$/.test(value.trim());
}

export function loadStudentId(): string {
  try {
    return window.localStorage.getItem(STUDENT_ID_KEY) ?? '';
  } catch {
    return '';
  }
}

export function saveStudentId(studentId: string): void {
  try {
    if (studentId) {
      window.localStorage.setItem(STUDENT_ID_KEY, studentId);
    } else {
      window.localStorage.removeItem(STUDENT_ID_KEY);
    }
  } catch {
  }
}

export function loadSelectedHoldId(): string {
  try {
    return window.localStorage.getItem(SELECTED_HOLD_ID_KEY) ?? '';
  } catch {
    return '';
  }
}

export function saveSelectedHoldId(holdId: string): void {
  try {
    if (holdId) {
      window.localStorage.setItem(SELECTED_HOLD_ID_KEY, holdId);
    } else {
      window.localStorage.removeItem(SELECTED_HOLD_ID_KEY);
    }
  } catch {
  }
}
