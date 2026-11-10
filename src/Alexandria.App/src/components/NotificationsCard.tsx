import { useEffect, useState } from 'react';
import { Text, View } from 'react-native';
import { ApiError, errorMessage } from '../api/client';
import { useSignedIn } from '../auth/AuthProvider';
import { disable, enable, getStatus, isIosOutsideHomeScreen, isSupported, type PushStatus } from '../notifications/webPush';
import { cards } from '../styles/cards';
import { text } from '../styles/typography';
import { Banner } from './Banner';
import { ToggleRow } from './ToggleRow';

const label = 'Borrow request notifications';

/**
 * Turns browser push notifications on or off for this device. Web only; hidden where push can't work
 * (native builds, unsupported browsers, or when the server has no push keys).
 */
export function NotificationsCard() {
  const { api } = useSignedIn();
  const [status, setStatus] = useState<PushStatus | null>(null);
  const [available, setAvailable] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isSupported() && !isIosOutsideHomeScreen()) {
      return;
    }
    let cancelled = false;
    (async () => {
      try {
        await api.getWebPushKey();
        const current = await getStatus();
        if (!cancelled) {
          setAvailable(true);
          setStatus(current);
        }
      } catch (e) {
        // 503 means the server has notifications turned off: hide the card. Anything else is worth showing.
        if (!cancelled && !(e instanceof ApiError && e.status === 503)) {
          setAvailable(true);
          setError(errorMessage(e));
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [api]);

  if (!available) {
    return null;
  }

  const toggle = async (on: boolean) => {
    setBusy(true);
    setError(null);
    try {
      setStatus(on ? await enable(api) : await disable(api));
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <View style={cards.card}>
      <Text style={text.label}>Notifications</Text>
      {status === 'needsHomeScreen' ? (
        <Text style={text.body}>
          To get notifications on iPhone or iPad, tap Share, then “Add to Home Screen”, and open Alexandria from there.
        </Text>
      ) : (
        <ToggleRow
          label={label}
          description="Get a notification on this device when a friend asks to borrow one of your books."
          value={status === 'on'}
          onChange={(on) => void toggle(on)}
          disabled={busy || status === 'blocked' || status === null}
        />
      )}
      {status === 'blocked' ? (
        <Text style={text.small}>Notifications are blocked for this site. Allow them in your browser’s site settings, then come back.</Text>
      ) : null}
      {error ? <Banner message={error} /> : null}
    </View>
  );
}
