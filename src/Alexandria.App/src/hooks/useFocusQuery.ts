import { useFocusEffect } from 'expo-router';
import { useCallback, useRef, useState } from 'react';
import { errorMessage } from '../api/client';

type QueryState<T> = {
  data: T | null;
  error: string | null;
  /** True only for the first load; later reloads keep showing the previous data. */
  loading: boolean;
  refreshing: boolean;
  reload: () => Promise<void>;
};

/**
 * Loads data when the screen gains focus (so returning from a detail screen shows fresh data),
 * and exposes `reload` for pull-to-refresh and after mutations.
 */
export function useFocusQuery<T>(load: () => Promise<T>): QueryState<T> {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const loadRef = useRef(load);
  loadRef.current = load;
  const hasData = useRef(false);

  const reload = useCallback(async () => {
    if (hasData.current) {
      setRefreshing(true);
    }
    try {
      const result = await loadRef.current();
      hasData.current = true;
      setData(result);
      setError(null);
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      void reload();
    }, [reload]),
  );

  return { data, error, loading, refreshing, reload };
}
