import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';
import type { User } from '../api/types';

export type Session = {
  token: string;
  expiresAt: string;
  user: User;
};

const key = 'alexandria.session';

/**
 * Persists the signed-in session. Native apps use the OS keychain/keystore (expo-secure-store);
 * the web build has no secure store, so it uses localStorage — the usual tradeoff for browser apps,
 * limited by the API token's 7-day lifetime.
 */
export const sessionStorage = {
  async load(): Promise<Session | null> {
    try {
      const raw = Platform.OS === 'web' ? globalThis.localStorage?.getItem(key) : await SecureStore.getItemAsync(key);
      if (!raw) {
        return null;
      }
      const session = JSON.parse(raw) as Session;
      return new Date(session.expiresAt).getTime() > Date.now() ? session : null;
    } catch {
      return null;
    }
  },

  async save(session: Session): Promise<void> {
    const raw = JSON.stringify(session);
    if (Platform.OS === 'web') {
      globalThis.localStorage?.setItem(key, raw);
    } else {
      await SecureStore.setItemAsync(key, raw);
    }
  },

  async clear(): Promise<void> {
    if (Platform.OS === 'web') {
      globalThis.localStorage?.removeItem(key);
    } else {
      await SecureStore.deleteItemAsync(key);
    }
  },
};
