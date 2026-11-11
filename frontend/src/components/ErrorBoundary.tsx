import { Component, type ErrorInfo, type ReactNode } from 'react';

interface ErrorBoundaryProps {
  children: ReactNode;
}

interface ErrorBoundaryState {
  errorMessage: string | null;
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { errorMessage: null };

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { errorMessage: error.message || 'An unexpected application error occurred.' };
  }

  componentDidCatch(error: Error, _errorInfo: ErrorInfo): void {
    void error;
  }

  render(): ReactNode {
    if (this.state.errorMessage) {
      return (
        <main className="page">
          <div className="alert alert-error" role="alert">
            <strong>Something went wrong:</strong> {this.state.errorMessage}
          </div>
          <button
            type="button"
            onClick={() => this.setState({ errorMessage: null })}
          >
            Try Again
          </button>
        </main>
      );
    }

    return this.props.children;
  }
}
