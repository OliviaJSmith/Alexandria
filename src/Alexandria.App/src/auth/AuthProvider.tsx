import { createContext, type ReactNode, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import { type AlexandriaApi, createAlexandriaApi } from '../api/alexandria';
import { createApiClient } from '../api/client';
import { disableQuietly } from '../notifications/webPush';
import type { User } from '../api/types';
import { type Session, sessionStorage } from './sessionStorage';

type AuthState =
  | { status: 'loading' }
  | { status: 'signedOut'; message?: string }
  | { status: 'signedIn'; session: Session };

type AuthContextValue = {
  state: AuthState;
  api: AlexandriaApi;
  /** Exchanges a Google access token for an Alexandria session (the caller has confirmed the beta terms). */
  signIn: (googleAccessToken: string) => Promise<void>;
  signOut: () => Promise<void>;
  /** Replaces the cached profile, e.g. after a username change. */
  updateUser: (user: User) => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({ status: 'loading' });
  const tokenRef = useRef<string | null>(null);

  const endSession = useCallback(async (message?: string) => {
    tokenRef.current = null;
    await sessionStorage.clear();
    setState({ status: 'signedOut', message });
  }, []);

  const api = useMemo(
    () =>
      createAlexandriaApi(
        createApiClient(
          () => tokenRef.current,
          () => void endSession('Your session has expired. Please sign in again.'),
        ),
      ),
    [endSession],
  );

  const startSession = useCallback(async (session: Session) => {
    tokenRef.current = session.token;
    await sessionStorage.save(session);
    setState({ status: 'signedIn', session });
  }, []);

  // Restore a saved session, then refresh the profile (an invalid token signs the user out via the 401 handler).
  useEffect(() => {
    let cancelled = false;
    (async () => {
      const saved = await sessionStorage.load();
      if (cancelled) {
        return;
      }
      if (!saved) {
        await endSession();
        return;
      }
      await startSession(saved);
      try {
        const user: User = await api.getCurrentUser();
        if (!cancelled) {
          await startSession({ ...saved, user });
        }
      } catch {
        // Offline or token rejected; the 401 handler already signed out if needed.
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [api, endSession, startSession]);

  const signIn = useCallback(
    async (googleAccessToken: string) => {
      const response = await api.signInWithGoogle(googleAccessToken);
      await startSession({ token: response.token, expiresAt: response.expiresAt, user: response.user });
    },
    [api, startSession],
  );

  const signOut = useCallback(async () => {
    // While still signed in, unsubscribe this browser so a shared device stops getting this user's notifications.
    await disableQuietly(api);
    await endSession();
  }, [api, endSession]);

  const updateUser = useCallback(
    async (user: User) => {
      if (state.status === 'signedIn') {
        await startSession({ ...state.session, user });
      }
    },
    [state, startSession],
  );

  const value = useMemo(() => ({ state, api, signIn, signOut, updateUser }), [state, api, signIn, signOut, updateUser]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside <AuthProvider>.');
  }
  return context;
}

/** The API client and signed-in user, for screens that are only reachable while signed in. */
export function useSignedIn(): { api: AlexandriaApi; user: User } {
  const { state, api } = useAuth();
  if (state.status !== 'signedIn') {
    throw new Error('useSignedIn used while not signed in.');
  }
  return { api, user: state.session.user };
}
