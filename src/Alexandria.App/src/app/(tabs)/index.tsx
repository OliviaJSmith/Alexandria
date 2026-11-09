import { router } from 'expo-router';
import { useState } from 'react';
import { Text, View } from 'react-native';
import { errorMessage } from '../../api/client';
import type { Library } from '../../api/types';
import { useSignedIn } from '../../auth/AuthProvider';
import { Banner } from '../../components/Banner';
import { Button } from '../../components/Button';
import { EmptyState } from '../../components/EmptyState';
import { ListItem } from '../../components/ListItem';
import { LoadingView } from '../../components/LoadingView';
import { Screen } from '../../components/Screen';
import { TextField } from '../../components/TextField';
import { ToggleRow } from '../../components/ToggleRow';
import { useFocusQuery } from '../../hooks/useFocusQuery';
import { cards } from '../../styles/cards';
import { layout } from '../../styles/layout';
import { text } from '../../styles/typography';

export default function LibrariesScreen() {
  const { api, user } = useSignedIn();
  const libraries = useFocusQuery(api.getMyLibraries);
  const [creating, setCreating] = useState(false);

  if (libraries.loading) {
    return <LoadingView message="Loading your libraries…" />;
  }

  const openLibrary = (library: Library) =>
    router.push({ pathname: '/library/[libraryId]', params: { libraryId: library.id } });

  return (
    <Screen onRefresh={libraries.reload} refreshing={libraries.refreshing}>
      <View style={layout.rowBetween}>
        <View style={layout.grow}>
          <Text style={text.title}>Your libraries</Text>
          <Text style={text.muted}>Signed in as {user.userName}</Text>
        </View>
        {!creating ? <Button label="New library" onPress={() => setCreating(true)} /> : null}
      </View>

      {libraries.error ? <Banner message={libraries.error} actionLabel="Retry" onAction={libraries.reload} /> : null}

      {creating ? (
        <NewLibraryForm
          onCancel={() => setCreating(false)}
          onCreated={(library) => {
            setCreating(false);
            void libraries.reload();
            openLibrary(library);
          }}
        />
      ) : null}

      {libraries.data?.length === 0 && !creating ? (
        <EmptyState
          title="No libraries yet"
          message="Create a library for each shelf, room or collection, then add your books to it."
        >
          <Button label="Create your first library" onPress={() => setCreating(true)} />
        </EmptyState>
      ) : null}

      <View style={layout.section}>
        {libraries.data?.map((library) => (
          <ListItem
            key={library.id}
            title={library.name}
            subtitle={library.isPublic ? 'Public · visible to your friends' : 'Private · only you'}
            onPress={() => openLibrary(library)}
          />
        ))}
      </View>
    </Screen>
  );
}

function NewLibraryForm({ onCancel, onCreated }: { onCancel: () => void; onCreated: (library: Library) => void }) {
  const { api } = useSignedIn();
  const [name, setName] = useState('');
  const [isPublic, setIsPublic] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const create = async () => {
    if (!name.trim()) {
      setError('Give the library a name.');
      return;
    }
    setSaving(true);
    setError(null);
    try {
      onCreated(await api.createLibrary(name.trim(), isPublic));
    } catch (e) {
      setError(errorMessage(e));
      setSaving(false);
    }
  };

  return (
    <View style={cards.card}>
      <Text style={text.heading}>New library</Text>
      <TextField
        label="Name"
        value={name}
        onChangeText={setName}
        placeholder="Living room shelf"
        maxLength={200}
        autoFocus
        returnKeyType="done"
        onSubmitEditing={create}
        error={error}
      />
      <ToggleRow
        label="Public"
        description="Public libraries are visible to your friends. Private ones only to you."
        value={isPublic}
        onChange={setIsPublic}
      />
      <View style={layout.row}>
        <Button label="Create" onPress={create} loading={saving} />
        <Button label="Cancel" variant="secondary" onPress={onCancel} disabled={saving} />
      </View>
    </View>
  );
}
