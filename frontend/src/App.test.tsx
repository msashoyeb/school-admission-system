import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from './App';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

describe('App', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it('persists student ID in localStorage and loads My Applications on mount', async () => {
    const studentId = '20152691234567890';
    window.localStorage.setItem('school_admission_student_id', studentId);

    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith('/classes')) {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'c_1',
              name: 'Class 6 - Morning Shift',
              totalSeats: 120,
              heldSeats: 1,
              confirmedSeats: 0,
              availableSeats: 119,
            },
          ]),
        );
      }
      if (url.endsWith('/me/holds')) {
        return Promise.resolve(
          jsonResponse([
            {
              holdId: 'h_1',
              classId: 'c_1',
              className: 'Class 6 - Morning Shift',
              status: 'HELD',
              expiresAt: new Date(Date.now() + 60_000).toISOString(),
            },
          ]),
        );
      }
      return Promise.resolve(jsonResponse([]));
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<App />);

    expect(screen.getByLabelText(/Student Birth Registration Number/i)).toHaveValue(studentId);
    expect(await screen.findByText('HELD')).toBeInTheDocument();
    expect(screen.getAllByText('Class 6 - Morning Shift').length).toBeGreaterThanOrEqual(2);
  });

  it('marks application EXPIRED and disables Pay Admission Fee without page reload when hold expires', async () => {
    vi.useFakeTimers();
    const baseTime = new Date('2026-11-10T10:00:00.000Z');
    vi.setSystemTime(baseTime);

    const studentId = '20152691234567890';
    window.localStorage.setItem('school_admission_student_id', studentId);

    const expiresAt = new Date(baseTime.getTime() + 2000).toISOString();

    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith('/classes')) {
        return Promise.resolve(jsonResponse([]));
      }
      if (url.endsWith('/me/holds')) {
        return Promise.resolve(
          jsonResponse([
            {
              holdId: 'h_exp',
              classId: 'c_1',
              className: 'Class 6 - Morning Shift',
              status: 'HELD',
              expiresAt,
            },
          ]),
        );
      }
      if (url.endsWith('/holds/h_exp')) {
        return Promise.resolve(
          jsonResponse({
            holdId: 'h_exp',
            classId: 'c_1',
            status: 'HELD',
            expiresAt,
          }),
        );
      }
      return Promise.resolve(jsonResponse([]));
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<App />);

    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(screen.queryByRole('heading', { name: 'Application Details' })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'View' }));

    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(screen.getByRole('heading', { name: 'Application Details' })).toBeInTheDocument();
    const payButtons = screen.getAllByRole('button', { name: 'Pay Admission Fee' });
    expect(screen.getAllByText('HELD').length).toBeGreaterThanOrEqual(1);
    expect(payButtons.every((button) => !button.hasAttribute('disabled'))).toBe(true);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(2500);
    });

    expect(screen.getAllByText('EXPIRED').length).toBeGreaterThanOrEqual(1);
    expect(
      screen.getAllByRole('button', { name: 'Pay Admission Fee' }).every((button) => button.hasAttribute('disabled')),
    ).toBe(true);
  });

  it('shows a user-friendly error message when the backend is unreachable or returns 409', async () => {
    let classesCallCount = 0;

    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = init?.method ?? 'GET';

      if (url.endsWith('/classes') && method === 'GET') {
        classesCallCount += 1;
        if (classesCallCount === 1) {
          return Promise.reject(new TypeError('Failed to fetch'));
        }
        return Promise.resolve(
          jsonResponse([
            {
              id: 'c_1',
              name: 'Class 6 - Morning Shift',
              totalSeats: 1,
              heldSeats: 0,
              confirmedSeats: 0,
              availableSeats: 1,
            },
          ]),
        );
      }

      if (url.endsWith('/classes/c_1/holds') && method === 'POST') {
        return Promise.resolve(
          jsonResponse({ error: 'No seats available for this class.' }, 409),
        );
      }

      return Promise.resolve(jsonResponse([]));
    });

    vi.stubGlobal('fetch', fetchMock);

    render(<App />);

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Cannot reach the admission server. Please check that the backend is running.',
    );

    const input = screen.getByLabelText(/Student Birth Registration Number/i);
    fireEvent.change(input, { target: { value: '20152691234567890' } });

    fireEvent.click(screen.getAllByRole('button', { name: 'Refresh' })[0]);
    const applyButton = await screen.findByRole('button', { name: 'Apply' });
    fireEvent.click(applyButton);

    expect(await screen.findByRole('alert')).toHaveTextContent('No seats available for this class.');
  });

  it('creates a class and submits X-Student-Id when applying for a seat', async () => {
    let classesState = [
      {
        id: 'c_1',
        name: 'Class 6 - Morning Shift',
        totalSeats: 10,
        heldSeats: 0,
        confirmedSeats: 0,
        availableSeats: 10,
      },
    ];

    let myHoldsState: Array<{
      holdId: string;
      classId: string;
      className: string;
      status: string;
      expiresAt: string;
    }> = [];

    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = init?.method ?? 'GET';

      if (url.endsWith('/classes') && method === 'GET') {
        return jsonResponse(classesState);
      }

      if (url.endsWith('/classes') && method === 'POST') {
        const body = JSON.parse(String(init?.body)) as { name: string; totalSeats: number };
        const created = { id: 'c_2', name: body.name, totalSeats: body.totalSeats };
        classesState = [
          ...classesState,
          { ...created, heldSeats: 0, confirmedSeats: 0, availableSeats: body.totalSeats },
        ];
        return jsonResponse(created, 201);
      }

      if (url.endsWith('/classes/c_1/holds') && method === 'POST') {
        const headers = init?.headers as Record<string, string>;
        const createdHold = {
          holdId: 'h_99',
          classId: 'c_1',
          studentId: headers['X-Student-Id'],
          status: 'HELD',
          expiresAt: new Date(Date.now() + 600_000).toISOString(),
        };
        myHoldsState = [
          {
            holdId: createdHold.holdId,
            classId: createdHold.classId,
            className: 'Class 6 - Morning Shift',
            status: createdHold.status,
            expiresAt: createdHold.expiresAt,
          },
        ];
        return jsonResponse(createdHold, 201);
      }

      if (url.endsWith('/me/holds')) {
        return jsonResponse(myHoldsState);
      }

      if (url.endsWith('/holds/h_99')) {
        return jsonResponse({
          holdId: 'h_99',
          classId: 'c_1',
          status: 'HELD',
          expiresAt: new Date(Date.now() + 600_000).toISOString(),
        });
      }

      return jsonResponse([]);
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<App />);

    expect(await screen.findByText('Class 6 - Morning Shift')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Create Class (School Admin)' })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'School Admin' }));
    expect(screen.queryByLabelText(/Student Birth Registration Number/i)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Apply' })).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Class Name'), {
      target: { value: 'Class 6 - Day Shift' },
    });
    fireEvent.change(screen.getByLabelText('Total Seats'), {
      target: { value: '60' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Create Class' }));

    expect(await screen.findByText('Class 6 - Day Shift')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Guardian' }));
    fireEvent.change(screen.getByLabelText(/Student Birth Registration Number/i), {
      target: { value: '20152691234567890' },
    });

    const applyButtons = screen.getAllByRole('button', { name: 'Apply' });
    fireEvent.click(applyButtons[0]);

    const viewButton = await screen.findByRole('button', { name: 'View' });
    expect(screen.queryByRole('heading', { name: 'Application Details' })).not.toBeInTheDocument();

    fireEvent.click(viewButton);
    await waitFor(() => {
      expect(screen.getByText('h_99')).toBeInTheDocument();
    });

    fireEvent.click(viewButton);
    expect(screen.queryByRole('heading', { name: 'Application Details' })).not.toBeInTheDocument();

    const holdCall = fetchMock.mock.calls.find(
      (call) => String(call[0]).endsWith('/classes/c_1/holds') && call[1]?.method === 'POST',
    );
    expect(holdCall).toBeDefined();
    const requestHeaders = holdCall?.[1]?.headers as Record<string, string> | undefined;
    expect(requestHeaders?.['X-Student-Id']).toBe('20152691234567890');
  });
});
