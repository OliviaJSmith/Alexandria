import { useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { Text, View } from 'react-native';
import { ApiError, errorMessage } from '../../../api/client';
import type { Book, CreateBookRequest } from '../../../api/types';
import { useSignedIn } from '../../../auth/AuthProvider';
import { Banner } from '../../../components/Banner';
import { type AddState, BookResultItem } from '../../../components/BookResultItem';
import { Button } from '../../../components/Button';
import { EmptyState } from '../../../components/EmptyState';
import { ManualBookForm } from '../../../components/ManualBookForm';
import { Screen } from '../../../components/Screen';
import { TextField } from '../../../components/TextField';
import { layout } from '../../../styles/layout';
import { text } from '../../../styles/typography';

/** Digits and hyphens only, 10 or 13 characters once cleaned (ISBN-10 may end in X). */
function looksLikeIsbn(input: string): boolean {
  const cleaned = input.replace(/[\s-]/g, '');
  return /^(\d{9}[\dXx]|\d{13})$/.test(cleaned);
}

const resultKey = (book: Book, index: number) => book.id ?? book.isbn ?? `${book.title}-${index}`;

export default function AddBookScreen() {
  const { libraryId } = useLocalSearchParams<{ libraryId: string }>();
  const { api } = useSignedIn();
  const [term, setTerm] = useState('');
  const [results, setResults] = useState<Book[] | null>(null);
  const [searching, setSearching] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [addStates, setAddStates] = useState<Record<string, AddState>>({});
  const [duplicate, setDuplicate] = useState<{ key: string; bookId: string; title: string } | null>(null);
  const [manual, setManual] = useState(false);
  const [manualSaving, setManualSaving] = useState(false);
  const [manualError, setManualError] = useState<string | null>(null);

  const search = async () => {
    const trimmed = term.trim();
    if (!trimmed) {
      return;
    }
    setSearching(true);
    setError(null);
    setDuplicate(null);
    try {
      if (looksLikeIsbn(trimmed)) {
        setResults([await api.lookupIsbn(trimmed)]);
      } else {
        setResults(await api.searchBooks(trimmed));
      }
      setAddStates({});
    } catch (e) {
      setResults(e instanceof ApiError && e.status === 404 ? [] : null);
      if (!(e instanceof ApiError && e.status === 404)) {
        setError(errorMessage(e));
      }
    } finally {
      setSearching(false);
    }
  };

  /** Creates the catalog entry if needed (external results have no id), then adds a copy to this library. */
  const ensureBookId = async (book: Book): Promise<string> => {
    if (book.id) {
      return book.id;
    }
    const { id: _ignored, ...fields } = book;
    const created = await api.createBook(fields);
    return created.id!;
  };

  /** Returns false when the library already has the book (the duplicate banner then offers another copy). */
  const addCopy = async (key: string, title: string, bookId: string, forceAdd: boolean): Promise<boolean> => {
    try {
      await api.addBookToLibrary(libraryId, bookId, forceAdd);
      setAddStates((states) => ({ ...states, [key]: 'added' }));
      setDuplicate(null);
      return true;
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) {
        setDuplicate({ key, bookId, title });
        setAddStates((states) => ({ ...states, [key]: 'idle' }));
        return false;
      }
      throw e;
    }
  };

  const add = async (book: Book, key: string) => {
    setAddStates((states) => ({ ...states, [key]: 'adding' }));
    setError(null);
    try {
      await addCopy(key, book.title, await ensureBookId(book), false);
    } catch (e) {
      setAddStates((states) => ({ ...states, [key]: 'idle' }));
      setError(errorMessage(e));
    }
  };

  const addManual = async (book: CreateBookRequest) => {
    setManualSaving(true);
    setManualError(null);
    try {
      // Creating returns the existing catalog entry when the ISBN is already known.
      const created = await api.createBook(book);
      const key = resultKey(created, 0);
      setManual(false);
      setResults((current) => [created, ...(current ?? []).filter((b, i) => resultKey(b, i) !== key)]);
      await addCopy(key, created.title, created.id!, false);
    } catch (e) {
      setManualError(errorMessage(e));
    } finally {
      setManualSaving(false);
    }
  };

  return (
    <Screen>
      <Text style={text.muted}>Search by title, author or ISBN. Books not in Alexandria yet come from Open Library and Google Books.</Text>
      <TextField
        label="Search"
        value={term}
        onChangeText={setTerm}
        placeholder="The Hobbit, Ursula K. Le Guin, 9780547928227…"
        returnKeyType="search"
        onSubmitEditing={search}
        autoFocus
        maxLength={200}
      />
      <View style={layout.row}>
        <Button label="Search" onPress={search} loading={searching} disabled={!term.trim()} />
        {!manual ? <Button label="Add by hand" variant="secondary" onPress={() => setManual(true)} /> : null}
      </View>

      {error ? <Banner message={error} /> : null}
      {duplicate ? (
        <Banner
          tone="info"
          message={`“${duplicate.title}” is already in this library.`}
          actionLabel="Add another copy"
          onAction={() => {
            setAddStates((states) => ({ ...states, [duplicate.key]: 'adding' }));
            addCopy(duplicate.key, duplicate.title, duplicate.bookId, true).catch((e) => setError(errorMessage(e)));
          }}
        />
      ) : null}

      {manual ? (
        <ManualBookForm saving={manualSaving} error={manualError} onSubmit={addManual} onCancel={() => setManual(false)} />
      ) : null}

      {results?.length === 0 ? (
        <EmptyState title="No matches" message="Try a different spelling or the ISBN on the back cover — or add the book by hand." />
      ) : null}

      <View style={layout.section}>
        {results?.map((book, index) => {
          const key = resultKey(book, index);
          return <BookResultItem key={key} book={book} addState={addStates[key] ?? 'idle'} onAdd={() => add(book, key)} />;
        })}
      </View>
    </Screen>
  );
}
