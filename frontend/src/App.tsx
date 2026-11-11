import { useCallback, useEffect, useState, type FormEvent } from 'react';
import {
  ApiError,
  cancelHold,
  confirmHold,
  createClass,
  createHold,
  getClasses,
  getHoldById,
  getMyHolds,
} from './api/admissionApi';
import type { ClassSummary, HoldDetail, MyHold } from './types/admission';
import {
  isValidStudentId,
  loadSelectedHoldId,
  loadStudentId,
  saveSelectedHoldId,
  saveStudentId,
} from './utils/studentStorage';
import { formatCountdown, getRemainingSeconds, resolveEffectiveStatus } from './utils/time';

function toErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  if (error instanceof Error && error.message) {
    return error.message;
  }
  return 'An unexpected error occurred.';
}

export function App() {
  const [studentId, setStudentId] = useState<string>(() => loadStudentId());
  const [classes, setClasses] = useState<ClassSummary[]>([]);
  const [myHolds, setMyHolds] = useState<MyHold[]>([]);
  const [selectedHoldId, setSelectedHoldId] = useState<string>(() => loadSelectedHoldId());
  const [activeHold, setActiveHold] = useState<HoldDetail | null>(null);

  const [className, setClassName] = useState('');
  const [totalSeatsInput, setTotalSeatsInput] = useState('');

  const [actionError, setActionError] = useState<string | null>(null);
  const [connectionError, setConnectionError] = useState<string | null>(null);
  const [noticeMessage, setNoticeMessage] = useState<string | null>(null);

  const [isCreatingClass, setIsCreatingClass] = useState(false);
  const [applyingClassId, setApplyingClassId] = useState<string | null>(null);
  const [busyHoldId, setBusyHoldId] = useState<string | null>(null);
  const [nowMs, setNowMs] = useState<number>(() => Date.now());

  const validStudentId = isValidStudentId(studentId);
  const trimmedStudentId = studentId.trim();

  const refreshClasses = useCallback(async () => {
    try {
      const data = await getClasses();
      setClasses(data);
      setConnectionError(null);
    } catch (error) {
      setConnectionError(toErrorMessage(error));
    }
  }, []);

  const refreshMyHolds = useCallback(async () => {
    if (!validStudentId) {
      setMyHolds([]);
      return;
    }

    try {
      const data = await getMyHolds(trimmedStudentId);
      setMyHolds(data);
      setConnectionError(null);
    } catch (error) {
      setConnectionError(toErrorMessage(error));
    }
  }, [validStudentId, trimmedStudentId]);

  const refreshSelectedHold = useCallback(async () => {
    if (!selectedHoldId) {
      setActiveHold(null);
      return;
    }

    try {
      const detail = await getHoldById(selectedHoldId);
      setActiveHold(detail);
      setConnectionError(null);
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        setSelectedHoldId('');
        saveSelectedHoldId('');
        setActiveHold(null);
      } else {
        setConnectionError(toErrorMessage(error));
      }
    }
  }, [selectedHoldId]);

  useEffect(() => {
    const timer = window.setInterval(() => {
      setNowMs(Date.now());
    }, 1000);
    return () => window.clearInterval(timer);
  }, []);

  useEffect(() => {
    let active = true;

    const load = async () => {
      try {
        const data = await getClasses();
        if (active) {
          setClasses(data);
          setConnectionError(null);
        }
      } catch (error) {
        if (active) {
          setConnectionError(toErrorMessage(error));
        }
      }
    };

    void load();
    const timer = window.setInterval(() => {
      void load();
    }, 2000);

    return () => {
      active = false;
      window.clearInterval(timer);
    };
  }, []);

  useEffect(() => {
    if (!validStudentId) {
      return;
    }

    let active = true;

    const load = async () => {
      try {
        const data = await getMyHolds(trimmedStudentId);
        if (active) {
          setMyHolds(data);
          setConnectionError(null);
        }
      } catch (error) {
        if (active) {
          setConnectionError(toErrorMessage(error));
        }
      }
    };

    void load();
    const timer = window.setInterval(() => {
      void load();
    }, 3000);

    return () => {
      active = false;
      window.clearInterval(timer);
    };
  }, [validStudentId, trimmedStudentId]);

  const shouldPollSelectedHold = Boolean(
    selectedHoldId &&
      (!activeHold || activeHold.holdId !== selectedHoldId || activeHold.status === 'HELD'),
  );

  useEffect(() => {
    if (!selectedHoldId) {
      return;
    }

    let active = true;

    const load = async () => {
      try {
        const detail = await getHoldById(selectedHoldId);
        if (active) {
          setActiveHold(detail);
          setConnectionError(null);
        }
      } catch (error) {
        if (!active) {
          return;
        }
        if (error instanceof ApiError && error.status === 404) {
          setSelectedHoldId('');
          saveSelectedHoldId('');
          setActiveHold(null);
        } else {
          setConnectionError(toErrorMessage(error));
        }
      }
    };

    void load();
    if (!shouldPollSelectedHold) {
      return () => {
        active = false;
      };
    }

    const timer = window.setInterval(() => {
      void load();
    }, 2000);

    return () => {
      active = false;
      window.clearInterval(timer);
    };
  }, [selectedHoldId, shouldPollSelectedHold]);

  function handleStudentIdChange(nextValue: string) {
    setStudentId(nextValue);
    saveStudentId(nextValue.trim());
    setActionError(null);
    if (!isValidStudentId(nextValue)) {
      setMyHolds([]);
    }
    if (nextValue.trim() !== trimmedStudentId) {
      setSelectedHoldId('');
      saveSelectedHoldId('');
      setActiveHold(null);
    }
  }

  async function handleCreateClass(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setActionError(null);
    setNoticeMessage(null);

    const trimmedName = className.trim();
    const parsedSeats = Number(totalSeatsInput);

    if (!trimmedName) {
      setActionError('Class name is required.');
      return;
    }

    if (!Number.isInteger(parsedSeats) || parsedSeats <= 0) {
      setActionError('Total seats must be a positive whole number.');
      return;
    }

    setIsCreatingClass(true);
    try {
      const created = await createClass({ name: trimmedName, totalSeats: parsedSeats });
      setClassName('');
      setTotalSeatsInput('');
      setNoticeMessage(`Created "${created.name}" with ${created.totalSeats} seats.`);
      await refreshClasses();
    } catch (error) {
      setActionError(toErrorMessage(error));
    } finally {
      setIsCreatingClass(false);
    }
  }

  async function handleApply(classItem: ClassSummary) {
    setActionError(null);
    setNoticeMessage(null);

    if (!validStudentId) {
      setActionError('Please enter a valid 17-digit birth registration number at the top of the page.');
      return;
    }

    setApplyingClassId(classItem.id);
    try {
      const hold = await createHold(classItem.id, trimmedStudentId);
      setSelectedHoldId(hold.holdId);
      saveSelectedHoldId(hold.holdId);
      setActiveHold({
        holdId: hold.holdId,
        classId: hold.classId,
        status: hold.status,
        expiresAt: hold.expiresAt,
      });
      setNowMs(Date.now());
      setNoticeMessage(`Seat held for "${classItem.name}". Complete payment before it expires.`);
      await Promise.all([refreshClasses(), refreshMyHolds()]);
    } catch (error) {
      setActionError(toErrorMessage(error));
      await refreshClasses();
    } finally {
      setApplyingClassId(null);
    }
  }

  async function handleConfirmHold(holdId: string) {
    setActionError(null);
    setNoticeMessage(null);

    if (!validStudentId) {
      setActionError('Please enter a valid 17-digit birth registration number.');
      return;
    }

    setBusyHoldId(holdId);
    try {
      const result = await confirmHold(holdId, trimmedStudentId);
      setNoticeMessage(`Admission confirmed (${result.holdId}).`);
      await Promise.all([refreshClasses(), refreshMyHolds(), refreshSelectedHold()]);
    } catch (error) {
      setActionError(toErrorMessage(error));
      await Promise.all([refreshClasses(), refreshMyHolds(), refreshSelectedHold()]);
    } finally {
      setBusyHoldId(null);
    }
  }

  async function handleCancelHold(holdId: string) {
    setActionError(null);
    setNoticeMessage(null);

    if (!validStudentId) {
      setActionError('Please enter a valid 17-digit birth registration number.');
      return;
    }

    setBusyHoldId(holdId);
    try {
      await cancelHold(holdId, trimmedStudentId);
      setNoticeMessage(`Application ${holdId} cancelled.`);
      await Promise.all([refreshClasses(), refreshMyHolds(), refreshSelectedHold()]);
    } catch (error) {
      setActionError(toErrorMessage(error));
      await Promise.all([refreshClasses(), refreshMyHolds(), refreshSelectedHold()]);
    } finally {
      setBusyHoldId(null);
    }
  }

  function findClassName(classId: string): string {
    const fromMyHolds = myHolds.find((h) => h.classId === classId)?.className;
    if (fromMyHolds) {
      return fromMyHolds;
    }

    return classes.find((c) => c.id === classId)?.name ?? classId;
  }

  const displayedError = actionError ?? connectionError;
  const activeStatus = activeHold
    ? resolveEffectiveStatus(activeHold.status, activeHold.expiresAt, nowMs)
    : null;
  const activeRemainingSeconds = activeHold
    ? getRemainingSeconds(activeHold.expiresAt, nowMs)
    : 0;

  return (
    <main className="page">
      <header className="header-card">
        <div>
          <h1>School Admission System</h1>
          <p className="muted"></p>
        </div>

        <div className="student-box">
          <label htmlFor="student-id-input">Student Birth Registration Number</label>
          <input
            id="student-id-input"
            type="text"
            inputMode="numeric"
            maxLength={17}
            placeholder=""
            value={studentId}
            onChange={(e) => handleStudentIdChange(e.target.value)}
          />
          <small className={validStudentId ? 'status-ok' : 'muted'}>
            {!trimmedStudentId
              ? 'Required before applying for a seat'
              : validStudentId
                ? 'Valid 17-digit Student ID'
                : 'Must be exactly 17 digits'}
          </small>
        </div>
      </header>

      {displayedError && (
        <div className="alert alert-error" role="alert">
          <span>{displayedError}</span>
          <button
            type="button"
            className="btn-link"
            onClick={() => {
              setActionError(null);
              setConnectionError(null);
            }}
          >
            Dismiss
          </button>
        </div>
      )}

      {noticeMessage && (
        <div className="alert alert-info" role="status">
          <span>{noticeMessage}</span>
          <button type="button" className="btn-link" onClick={() => setNoticeMessage(null)}>
            Dismiss
          </button>
        </div>
      )}

      <section className="card">
        <h2>Create Class (School Admin)</h2>
        <form className="inline-form" onSubmit={handleCreateClass}>
          <div className="field">
            <label htmlFor="class-name-input">Class Name</label>
            <input
              id="class-name-input"
              type="text"
              placeholder=""
              value={className}
              onChange={(e) => setClassName(e.target.value)}
            />
          </div>

          <div className="field field-small">
            <label htmlFor="total-seats-input">Total Seats</label>
            <input
              id="total-seats-input"
              type="number"
              min={1}
              placeholder=""
              value={totalSeatsInput}
              onChange={(e) => setTotalSeatsInput(e.target.value)}
            />
          </div>

          <button type="submit" disabled={isCreatingClass}>
            {isCreatingClass ? 'Creating...' : 'Create Class'}
          </button>
        </form>
      </section>

      <section className="card">
        <div className="section-head">
          <h2>Class List</h2>
          <button type="button" className="btn-secondary" onClick={() => void refreshClasses()}>
            Refresh
          </button>
        </div>

        {classes.length === 0 ? (
          <p className="muted">No classes created yet.</p>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Class</th>
                <th>Total</th>
                <th>Held</th>
                <th>Confirmed</th>
                <th>Available</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {classes.map((item) => (
                <tr key={item.id}>
                  <td>{item.name}</td>
                  <td>{item.totalSeats}</td>
                  <td>{item.heldSeats}</td>
                  <td>{item.confirmedSeats}</td>
                  <td>{item.availableSeats}</td>
                  <td>
                    <button
                      type="button"
                      disabled={item.availableSeats <= 0 || applyingClassId === item.id}
                      onClick={() => void handleApply(item)}
                    >
                      {applyingClassId === item.id
                        ? 'Applying...'
                        : item.availableSeats <= 0
                          ? 'Full'
                          : 'Apply'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      {activeHold && activeStatus && (
        <section className="card">
          <h2>Application Details</h2>
          <div className="detail-grid">
            <div>
              <strong>Class:</strong> {findClassName(activeHold.classId)}
            </div>
            <div>
              <strong>Hold ID:</strong> {activeHold.holdId}
            </div>
            <div>
              <strong>Status:</strong>{' '}
              <span className={`badge badge-${activeStatus.toLowerCase()}`}>{activeStatus}</span>
            </div>
            <div>
              <strong>Time Remaining:</strong>{' '}
              {activeStatus === 'HELD' ? formatCountdown(activeRemainingSeconds) : '00:00'}
            </div>
          </div>

          <div className="actions-row">
            <button
              type="button"
              disabled={activeStatus !== 'HELD' || busyHoldId === activeHold.holdId}
              onClick={() => void handleConfirmHold(activeHold.holdId)}
            >
              {busyHoldId === activeHold.holdId ? 'Processing...' : 'Pay Admission Fee'}
            </button>
            <button
              type="button"
              className="btn-secondary"
              disabled={activeStatus !== 'HELD' || busyHoldId === activeHold.holdId}
              onClick={() => void handleCancelHold(activeHold.holdId)}
            >
              Cancel
            </button>
          </div>
        </section>
      )}

      <section className="card">
        <div className="section-head">
          <h2>My Applications</h2>
          {validStudentId && (
            <button type="button" className="btn-secondary" onClick={() => void refreshMyHolds()}>
              Refresh
            </button>
          )}
        </div>

        {!validStudentId ? (
          <p className="muted"></p>
        ) : myHolds.length === 0 ? (
          <p className="muted">No applications found for {trimmedStudentId}.</p>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Class</th>
                <th>Status</th>
                <th>Time Remaining</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {myHolds.map((hold) => {
                const status = resolveEffectiveStatus(hold.status, hold.expiresAt, nowMs);
                const remaining = getRemainingSeconds(hold.expiresAt, nowMs);
                const isHeld = status === 'HELD';
                const isBusy = busyHoldId === hold.holdId;

                return (
                  <tr key={hold.holdId}>
                    <td>{hold.className || hold.classId}</td>
                    <td>
                      <span className={`badge badge-${status.toLowerCase()}`}>{status}</span>
                    </td>
                    <td>{isHeld ? formatCountdown(remaining) : '-'}</td>
                    <td className="actions-cell">
                      <button
                        type="button"
                        className="btn-secondary"
                        onClick={() => {
                          setSelectedHoldId(hold.holdId);
                          saveSelectedHoldId(hold.holdId);
                          setActiveHold({
                            holdId: hold.holdId,
                            classId: hold.classId,
                            status: hold.status,
                            expiresAt: hold.expiresAt,
                          });
                        }}
                      >
                        View
                      </button>
                      <button
                        type="button"
                        disabled={!isHeld || isBusy}
                        onClick={() => void handleConfirmHold(hold.holdId)}
                      >
                        Pay Admission Fee
                      </button>
                      <button
                        type="button"
                        className="btn-secondary"
                        disabled={!isHeld || isBusy}
                        onClick={() => void handleCancelHold(hold.holdId)}
                      >
                        Cancel
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </section>
    </main>
  );
}

export default App;
