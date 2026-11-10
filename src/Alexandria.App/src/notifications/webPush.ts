import { Platform } from 'react-native';
import type { AlexandriaApi } from '../api/alexandria';

/** Browser notification state, from this browser's point of view. */
export type PushStatus = 'unsupported' | 'needsHomeScreen' | 'blocked' | 'on' | 'off';

const serviceWorkerPath = '/sw.js';

/** True when this browser can do Web Push at all (never on native builds). */
export function isSupported(): boolean {
  return (
    Platform.OS === 'web' &&
    typeof window !== 'undefined' &&
    'serviceWorker' in navigator &&
    'PushManager' in window &&
    'Notification' in window
  );
}

/** iPhone/iPad Safari only allows push for sites added to the Home Screen. */
export function isIosOutsideHomeScreen(): boolean {
  if (Platform.OS !== 'web' || typeof navigator === 'undefined') {
    return false;
  }
  const ios = /iPad|iPhone|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
  const standalone = (navigator as Navigator & { standalone?: boolean }).standalone === true;
  return ios && !standalone;
}

export async function getStatus(): Promise<PushStatus> {
  if (isIosOutsideHomeScreen()) {
    return 'needsHomeScreen';
  }
  if (!isSupported()) {
    return 'unsupported';
  }
  if (Notification.permission === 'denied') {
    return 'blocked';
  }
  const subscription = await currentSubscription();
  return subscription && Notification.permission === 'granted' ? 'on' : 'off';
}

/**
 * Asks for permission and subscribes this browser. Call it from a button press: browsers (iOS especially)
 * only show the permission prompt in response to a tap.
 */
export async function enable(api: AlexandriaApi): Promise<PushStatus> {
  const permission = await Notification.requestPermission();
  if (permission !== 'granted') {
    return permission === 'denied' ? 'blocked' : 'off';
  }

  const { publicKey } = await api.getWebPushKey();
  const registration = await navigator.serviceWorker.register(serviceWorkerPath);
  await navigator.serviceWorker.ready;
  const subscription =
    (await registration.pushManager.getSubscription()) ??
    (await registration.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: fromBase64Url(publicKey) }));

  const keys = subscription.toJSON().keys ?? {};
  if (!keys.p256dh || !keys.auth) {
    throw new Error('This browser returned an incomplete push subscription.');
  }
  await api.subscribePush({ endpoint: subscription.endpoint, p256dh: keys.p256dh, auth: keys.auth });
  return 'on';
}

/** Stops notifications in this browser and tells the API to forget it. */
export async function disable(api: AlexandriaApi): Promise<PushStatus> {
  const subscription = await currentSubscription();
  if (subscription) {
    const endpoint = subscription.endpoint;
    await subscription.unsubscribe();
    try {
      await api.unsubscribePush(endpoint);
    } catch {
      // Already gone on the server; the browser side is what stops delivery.
    }
  }
  return 'off';
}

/** Best effort on sign-out, so a shared browser stops getting the previous user's notifications. Never throws. */
export async function disableQuietly(api: AlexandriaApi): Promise<void> {
  if (!isSupported()) {
    return;
  }
  try {
    await disable(api);
  } catch {
    // Sign-out must not fail because of notifications.
  }
}

async function currentSubscription(): Promise<PushSubscription | null> {
  const registration = await navigator.serviceWorker.getRegistration(serviceWorkerPath);
  return (await registration?.pushManager.getSubscription()) ?? null;
}

function fromBase64Url(value: string): Uint8Array<ArrayBuffer> {
  const base64 = value.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(value.length / 4) * 4, '=');
  const binary = atob(base64);
  const bytes = new Uint8Array(new ArrayBuffer(binary.length));
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes;
}
