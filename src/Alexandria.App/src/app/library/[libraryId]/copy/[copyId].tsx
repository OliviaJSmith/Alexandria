import { router, Stack, useLocalSearchParams } from 'expo-router';
import { useCallback, useEffect, useState } from 'react';
import { Text, View } from 'react-native';
import { errorMessage } from '../../../../api/client';
import { libraryBookStatuses, statusLabels, type LibraryBook, type LibraryBookStatus } from '../../../../api/types';
import { useSignedIn } from '../../../../auth/AuthProvider';
import { Banner } from '../../../../components/Banner';
import { BookDetails } from '../../../../components/BookDetails';
import { Button } from '../../../../components/Button';
import { ChipGroup } from '../../../../components/ChipGroup';
import { ConfirmButton } from '../../../../components/ConfirmButton';
import { LendCard } from '../../../../components/LendCard';
import { LoadingView } from '../../../../components/LoadingView';
import { Screen } from '../../../../components/Screen';
import { TextField } from '../../../../components/TextField';
import { useFocusQuery } from '../../../../hooks/useFocusQuery';
import { cards } from '../../../../styles/cards';
import { layout } from '../../../../styles/layout';
import { text } from '../../../../styles/typography';

const statusOptions = libraryBookStatuses.map((status) => ({ value: status, label: statusLabels[status] }));

export default function CopyScreen() {
  const { libraryId, copyId } = useLocalSearchParams<{ libraryId: string; copyId: string }>();
  const { api } = useSignedIn();
  const load = useCallback(async () => {
    const [copies, libraries, activeLoans, friends] = await Promise.all([
      api.getLibraryBooks(libraryId),
      api.getMyLibraries(),
      api.getLoans('Lender', 'Active'),
      api.getFriends(),
    ]);
    return {
      copy: copies.find((c) => c.id === copyId) ?? null,
      libraries,
      activeLoan: activeLoans.find((l) => l.libraryBookId === copyId) ?? null,
      friends,
    };
  }, [api, libraryId, copyId]);
  const query = useFocusQuery(load);

  const [loanNote, setLoanNote] = useState('');
  const [genre, setGenre] = useState('');
  const [targetLibraryId, setTargetLibraryId] = useState<string | null>(null);
  const [busy, setBusy] = useState<null | 'status' | 'details' | 'move' | 'remove'>(null);
  const [error, setError] = useState<string | null>(null);

  const copy = query.data?.copy ?? null;
  useEffect(() => {
    setLoanNote(copy?.loanNote ?? '');
    setGenre(copy?.book.genre ?? '');
  }, [copy?.loanNote, copy?.book.genre]);

  if (query.loading) {
    return <LoadingView message="Loading book…" />;
  }

  if (!copy) {
    return (
      <Screen>
        <Banner message={query.error ?? 'This book is no longer in the library.'} actionLabel="Back" onAction={() => router.back()} />
      </Screen>
    );
  }

  const run = async (action: typeof busy, work: () => Promise<unknown>, after?: () => void) => {
    setBusy(action);
    setError(null);
    try {
      await work();
      after ? after() : await query.reload();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setBusy(null);
    }
  };

  const otherLibraries = (query.data?.libraries ?? []).filter((library) => library.id !== libraryId);
  const lentOut = copy.status === 'CheckedOut';
  const update = (changes: Parameters<typeof api.updateLibraryBook>[2]) => api.updateLibraryBook(libraryId, copy.id, changes);

  return (
    <Screen onRefresh={query.reload} refreshing={query.refreshing}>
      <Stack.Screen options={{ title: copy.book.title }} />
      <BookDetails book={copy.book} />
      {error ? <Banner message={error} /> : null}

      <LendCard
        copy={copy}
        activeLoan={query.data?.activeLoan ?? null}
        friends={query.data?.friends ?? []}
        onChanged={query.reload}
      />

      <View style={cards.card}>
        <Text style={text.label}>Status</Text>
        <ChipGroup<LibraryBookStatus>
          accessibilityLabel="Status"
          options={statusOptions}
          value={copy.status}
          disabled={busy !== null}
          onChange={(status) => run('status', () => update({ status }))}
        />
      </View>

      <View style={cards.card}>
        <TextField
          label="Loan note"
          value={loanNote}
          onChangeText={setLoanNote}
          placeholder="Who has it, and since when?"
          maxLength={500}
          multiline
        />
        <TextField label="Genre" value={genre} onChangeText={setGenre} maxLength={100} />
        <Text style={text.small}>Genre is shared by everyone who owns this book.</Text>
        <Button
          label="Save details"
          variant="secondary"
          loading={busy === 'details'}
          disabled={busy !== null || (loanNote === (copy.loanNote ?? '') && genre === (copy.book.genre ?? ''))}
          onPress={() => run('details', () => update({ loanNote, genre }))}
        />
      </View>

      {otherLibraries.length > 0 ? (
        <MoveCard
          copy={copy}
          libraries={otherLibraries}
          targetLibraryId={targetLibraryId}
          onSelect={setTargetLibraryId}
          moving={busy === 'move'}
          disabled={busy !== null || lentOut}
          onMove={() =>
            targetLibraryId &&
            run(
              'move',
              () => api.moveLibraryBook(libraryId, copy.id, targetLibraryId),
              () => router.replace({ pathname: '/library/[libraryId]', params: { libraryId: targetLibraryId } }),
            )
          }
        />
      ) : null}

      <View style={cards.card}>
        <Text style={text.label}>Remove</Text>
        {lentOut ? <Text style={text.small}>Mark the book as returned before removing it.</Text> : null}
        <ConfirmButton
          label="Remove from library"
          confirmLabel="Tap again to remove"
          loading={busy === 'remove'}
          disabled={busy !== null || lentOut}
          onConfirm={() => run('remove', () => api.removeLibraryBook(libraryId, copy.id), () => router.back())}
        />
      </View>
    </Screen>
  );
}

type MoveCardProps = {
  copy: LibraryBook;
  libraries: { id: string; name: string }[];
  targetLibraryId: string | null;
  onSelect: (libraryId: string) => void;
  onMove: () => void;
  moving: boolean;
  disabled: boolean;
};

function MoveCard({ copy, libraries, targetLibraryId, onSelect, onMove, moving, disabled }: MoveCardProps) {
  return (
    <View style={cards.card}>
      <Text style={text.label}>Move to another library</Text>
      {copy.status === 'CheckedOut' ? <Text style={text.small}>Mark the book as returned before moving it.</Text> : null}
      <ChipGroup
        accessibilityLabel="Target library"
        options={libraries.map((library) => ({ value: library.id, label: library.name }))}
        value={targetLibraryId}
        onChange={onSelect}
        disabled={disabled}
      />
      <View style={layout.row}>
        <Button label="Move" variant="secondary" onPress={onMove} loading={moving} disabled={disabled || !targetLibraryId} />
      </View>
    </View>
  );
}
