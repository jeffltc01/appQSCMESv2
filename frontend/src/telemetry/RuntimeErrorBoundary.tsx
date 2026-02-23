import React from 'react';
import { reportException } from './telemetryClient.ts';

interface RuntimeErrorBoundaryProps {
  children: React.ReactNode;
}

interface RuntimeErrorBoundaryState {
  hasError: boolean;
}

export class RuntimeErrorBoundary extends React.Component<
  RuntimeErrorBoundaryProps,
  RuntimeErrorBoundaryState
> {
  constructor(props: RuntimeErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(): RuntimeErrorBoundaryState {
    return { hasError: true };
  }

  componentDidCatch(error: Error, info: React.ErrorInfo) {
    reportException(error, {
      category: 'react_render',
      source: 'error_boundary',
      severity: 'error',
      isReactRuntimeOverlayCandidate: true,
      metadataJson: JSON.stringify({ componentStack: info.componentStack }),
    });
  }

  render() {
    if (this.state.hasError) {
      return (
        <div style={{ padding: 24 }}>
          <h2>Something went wrong.</h2>
          <p>Please reload the application.</p>
        </div>
      );
    }
    return this.props.children;
  }
}
