import { router, Stack, useLocalSearchParams } from 'expo-router';
import { useCallback, useState } from 'react';
import { Text, View } from 'react-native';
import { errorMessage } from '../../../api/client';
import type { Library } from '../../../api/types';
import { useSignedIn } from '../../../auth/AuthProvider';
import { Banner } from '../../../components/Banner';
import { BorrowButton } from '../../../components/BorrowButton';
import { Button } from '../../../components/Button';
import { EmptyState } from '../../../components/EmptyState';
import { LibraryBookItem } from '../../../components/LibraryBookItem';
import { LoadingView } from '../../../components/LoadingView';
import { Screen } from '../../../components/Screen';
import { TextField } from '../../../components/TextField';
import { ToggleRow } from '../../../components/ToggleRow';
import { useFocusQuery } from '../../../hooks/useFocusQuery';
import { cards } from '../../../styles/cards';
import { layout } from '../../../styles/layout';
import { text } from '../../../styles/typography';

export default function LibraryScreen() {
  const { libraryId } = useLocalSearchParams<{ libraryId: string }>();
  const { api, user } = useSignedIn();
  const load = useCallback(
    async () => {
      const [library, copies] = await Promise.all([api.getLibrary(libraryId), api.getLibraryBooks(libraryId)]);
      return { library, copies };
    },
    [api, libraryId],
  );
  const query = useFocusQuery(load);
  const [borrowError, setBorrowError] = useState<string | null>(null);

  if (query.loading) {
    return <LoadingView message="Opening library…" />;
  }

  if (!query.data) {
    return (
      <Screen>
        <Banner message={query.error ?? 'This library could not be loaded.'} actionLabel="Retry" onAction={query.reload} />
      </Screen>
    );
  }

  const { library, copies } = query.data;
  const isOwner = library.ownerId === user.id;

  return (
    <Screen onRefresh={query.reload} refreshing={query.refreshing}>
      <Stack.Screen options={{ title: library.name }} />
      {query.error ? <Banner message={query.error} /> : null}
      {borrowError ? <Banner message={borrowError} /> : null}

      {isOwner ? <LibrarySettings library={library} onSaved={query.reload} /> : <Text style={text.title}>{library.name}</Text>}

      <View style={layout.rowBetween}>
        <Text style={text.heading}>
          {copies.length} {copies.length === 1 ? 'book' : 'books'}
        </Text>
        {isOwner ? (
          <View style={layout.row}>
            <Button
              label="Scan"
              variant="secondary"
              onPress={() => router.push({ pathname: '/library/[libraryId]/scan', params: { libraryId: library.id } })}
              accessibilityHint="Add books from a photo of a cover or shelf"
            />
            <Button
              label="Add a book"
              onPress={() => router.push({ pathname: '/library/[libraryId]/add-book', params: { libraryId: library.id } })}
            />
          </View>
        ) : null}
      </View>

      {copies.length === 0 ? (
        <EmptyState
          title="This library is empty"
          message={isOwner ? 'Search by title, author or ISBN to add your first book.' : 'No books here yet.'}
        />
      ) : null}

      <View style={layout.section}>
        {copies.map((copy) => (
          <LibraryBookItem
            key={copy.id}
            copy={copy}
            onPress={
              isOwner
                ? () =>
                    router.push({
                      pathname: '/library/[libraryId]/copy/[copyId]',
                      params: { libraryId: library.id, copyId: copy.id },
                    })
                : undefined
            }
            trailing={isOwner ? undefined : <BorrowButton copy={copy} onError={setBorrowError} />}
          />
        ))}
      </View>
    </Screen>
  );
}

/** Owner-only: rename the library and switch it between public and private. */
function LibrarySettings({ library, onSaved }: { library: Library; onSaved: () => Promise<void> }) {
  const { api } = useSignedIn();
  const [editingName, setEditingName] = useState(false);
  const [name, setName] = useState(library.name);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const save = async (changes: { name?: string; isPublic?: boolean }) => {
    setSaving(true);
    setError(null);
    try {
      await api.updateLibrary(library.id, changes);
      setEditingName(false);
      await onSaved();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSaving(false);
    }
  };

  return (
    <View style={cards.card}>
      {editingName ? (
        <>
          <TextField label="Library name" value={name} onChangeText={setName} maxLength={200} autoFocus />
          <View style={layout.row}>
            <Button label="Save" onPress={() => save({ name: name.trim() })} loading={saving} disabled={!name.trim()} />
            <Button
              label="Cancel"
              variant="secondary"
              disabled={saving}
              onPress={() => {
                setName(library.name);
                setEditingName(false);
              }}
            />
          </View>
        </>
      ) : (
        <View style={layout.rowBetween}>
          <Text style={[text.title, layout.grow]}>{library.name}</Text>
          <Button label="Rename" variant="secondary" onPress={() => setEditingName(true)} />
        </View>
      )}
      <ToggleRow
        label="Public"
        description={library.isPublic ? 'Your friends can see this library.' : 'Only you can see this library.'}
        value={library.isPublic}
        disabled={saving}
        onChange={(isPublic) => save({ isPublic })}
      />
      {error ? <Text style={text.error}>{error}</Text> : null}
    </View>
  );
}
