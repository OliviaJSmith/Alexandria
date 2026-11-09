import { useEffect, useState } from 'react';
import { Text, View } from 'react-native';
import { errorMessage } from '../api/client';
import { useAuth, useSignedIn } from '../auth/AuthProvider';
import { cards } from '../styles/cards';
import { layout } from '../styles/layout';
import { text } from '../styles/typography';
import { Button } from './Button';
import { TextField } from './TextField';

const validPattern = /^[A-Za-z0-9_]{3,30}$/;

/** Change your username; checks availability as you type (debounced). */
export function UserNameForm() {
  const { api, user } = useSignedIn();
  const { updateUser } = useAuth();
  const [userName, setUserName] = useState(user.userName);
  const [hint, setHint] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  const trimmed = userName.trim();
  const unchanged = trimmed === user.userName;
  const valid = validPattern.test(trimmed);

  useEffect(() => {
    setSaved(false);
    if (unchanged || !valid) {
      setHint(valid || unchanged ? null : 'Use 3–30 letters, numbers or underscores.');
      return;
    }
    let cancelled = false;
    const timer = setTimeout(async () => {
      try {
        const availability = await api.checkUserName(trimmed);
        if (!cancelled) {
          setHint(availability.isAvailable ? `@${trimmed} is available.` : `@${trimmed} is taken.`);
        }
      } catch {
        // Availability is a convenience; saving reports the real answer.
      }
    }, 400);
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [api, trimmed, unchanged, valid]);

  const save = async () => {
    setSaving(true);
    setError(null);
    try {
      await updateUser(await api.updateUserName(trimmed));
      setSaved(true);
      setHint(null);
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSaving(false);
    }
  };

  return (
    <View style={cards.card}>
      <TextField
        label="Username"
        value={userName}
        onChangeText={setUserName}
        autoCapitalize="none"
        autoCorrect={false}
        maxLength={30}
        error={error}
      />
      {hint ? <Text style={text.small}>{hint}</Text> : null}
      {saved ? <Text style={text.small}>Saved. Friends can now find you as @{user.userName}.</Text> : null}
      <View style={layout.row}>
        <Button label="Save username" variant="secondary" onPress={save} loading={saving} disabled={unchanged || !valid} />
      </View>
    </View>
  );
}
