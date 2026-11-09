import * as ImagePicker from 'expo-image-picker';
import { router, useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { Text, View } from 'react-native';
import { ApiError, errorMessage } from '../../../api/client';
import type { CreateBookRequest, PhotoUpload, ScanMode, ScanResult, ScannedBook } from '../../../api/types';
import { useSignedIn } from '../../../auth/AuthProvider';
import { Banner } from '../../../components/Banner';
import { Button } from '../../../components/Button';
import { ChipGroup } from '../../../components/ChipGroup';
import { EmptyState } from '../../../components/EmptyState';
import { ScanResultItem } from '../../../components/ScanResultItem';
import { Screen } from '../../../components/Screen';
import { cards } from '../../../styles/cards';
import { layout } from '../../../styles/layout';
import { text } from '../../../styles/typography';

const modeOptions: { value: ScanMode; label: string }[] = [
  { value: 'Single', label: 'One book' },
  { value: 'Shelf', label: 'A shelf of spines' },
];

const pickerOptions: ImagePicker.ImagePickerOptions = { mediaTypes: ['images'], quality: 0.8 };

function toCreateRequest(scanned: ScannedBook): CreateBookRequest {
  if (scanned.match) {
    const { id: _ignored, ...fields } = scanned.match;
    return fields;
  }
  return {
    title: scanned.detectedTitle,
    author: scanned.detectedAuthor,
    isbn: scanned.detectedIsbn,
    publisher: null,
    publishedYear: null,
    description: null,
    coverImageUrl: null,
    genre: null,
    pageCount: null,
  };
}

export default function ScanScreen() {
  const { libraryId } = useLocalSearchParams<{ libraryId: string }>();
  const { api } = useSignedIn();
  const [mode, setMode] = useState<ScanMode>('Single');
  const [scanning, setScanning] = useState(false);
  const [result, setResult] = useState<ScanResult | null>(null);
  const [selected, setSelected] = useState<boolean[]>([]);
  const [adding, setAdding] = useState(false);
  const [summary, setSummary] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const pick = async (source: 'library' | 'camera') => {
    setError(null);
    setSummary(null);
    if (source === 'camera') {
      const permission = await ImagePicker.requestCameraPermissionsAsync();
      if (!permission.granted) {
        setError('Camera access is needed to take a photo. You can choose an existing photo instead.');
        return;
      }
    }
    const picked = source === 'camera' ? await ImagePicker.launchCameraAsync(pickerOptions) : await ImagePicker.launchImageLibraryAsync(pickerOptions);
    if (picked.canceled || picked.assets.length === 0) {
      return;
    }
    const asset = picked.assets[0];
    await scan({ uri: asset.uri, file: asset.file, mimeType: asset.mimeType, fileName: asset.fileName });
  };

  const scan = async (photo: PhotoUpload) => {
    setScanning(true);
    setResult(null);
    try {
      const scanned = await api.scanPhoto(photo, mode);
      setResult(scanned);
      setSelected(scanned.books.map((b) => b.match !== null));
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setScanning(false);
    }
  };

  const addSelected = async () => {
    if (!result) {
      return;
    }
    setAdding(true);
    setError(null);
    let added = 0;
    let alreadyThere = 0;
    const failed: string[] = [];
    for (const [index, scanned] of result.books.entries()) {
      if (!selected[index]) {
        continue;
      }
      try {
        const bookId = scanned.match?.id ?? (await api.createBook(toCreateRequest(scanned))).id!;
        await api.addBookToLibrary(libraryId, bookId);
        added++;
      } catch (e) {
        if (e instanceof ApiError && e.status === 409) {
          alreadyThere++;
        } else {
          failed.push(scanned.match?.title ?? scanned.detectedTitle);
        }
      }
    }
    setAdding(false);
    setSummary(
      [
        `Added ${added} ${added === 1 ? 'book' : 'books'}.`,
        alreadyThere ? `${alreadyThere} already in this library.` : '',
        failed.length ? `Couldn't add: ${failed.join(', ')}.` : '',
      ].filter(Boolean).join(' '),
    );
    setResult(null);
  };

  const selectedCount = selected.filter(Boolean).length;

  return (
    <Screen>
      <Text style={text.muted}>Take or choose a photo. Alexandria reads the titles and finds each book for you to confirm.</Text>
      <View style={cards.card}>
        <Text style={text.label}>What's in the photo?</Text>
        <ChipGroup accessibilityLabel="Photo type" options={modeOptions} value={mode} onChange={setMode} disabled={scanning} />
        <View style={layout.rowWrap}>
          <Button label="Take a photo" onPress={() => pick('camera')} disabled={scanning} />
          <Button label="Choose a photo" variant="secondary" onPress={() => pick('library')} disabled={scanning} />
        </View>
        {scanning ? <Text style={text.muted}>Reading your photo… this can take up to a minute.</Text> : null}
      </View>

      {error ? <Banner message={error} /> : null}
      {summary ? <Banner tone="info" message={summary} actionLabel="Back to library" onAction={() => router.back()} /> : null}

      {result && result.books.length === 0 ? <EmptyState title="No books found" message={result.message ?? undefined} /> : null}
      {result && result.books.length > 0 ? (
        <View style={layout.section}>
          <Text style={text.heading}>Found {result.books.length} {result.books.length === 1 ? 'book' : 'books'}</Text>
          {result.books.map((scanned, index) => (
            <ScanResultItem
              key={`${scanned.detectedTitle}-${index}`}
              scanned={scanned}
              selected={selected[index] ?? false}
              onToggle={(value) => setSelected((current) => current.map((s, i) => (i === index ? value : s)))}
            />
          ))}
          <Button
            label={`Add ${selectedCount} ${selectedCount === 1 ? 'book' : 'books'} to library`}
            onPress={addSelected}
            loading={adding}
            disabled={selectedCount === 0}
            fullWidth
          />
        </View>
      ) : null}
    </Screen>
  );
}
