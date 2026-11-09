import { useState } from 'react';
import { Text, View } from 'react-native';
import type { CreateBookRequest } from '../api/types';
import { cards } from '../styles/cards';
import { layout } from '../styles/layout';
import { text } from '../styles/typography';
import { Button } from './Button';
import { TextField } from './TextField';

type ManualBookFormProps = {
  saving: boolean;
  error: string | null;
  onSubmit: (book: CreateBookRequest) => void;
  onCancel: () => void;
};

/** For books no catalog knows about (family recipes, self-published, very old editions). */
export function ManualBookForm({ saving, error, onSubmit, onCancel }: ManualBookFormProps) {
  const [title, setTitle] = useState('');
  const [author, setAuthor] = useState('');
  const [isbn, setIsbn] = useState('');
  const [year, setYear] = useState('');

  const submit = () => {
    const publishedYear = Number.parseInt(year, 10);
    onSubmit({
      title: title.trim(),
      author: author.trim() || null,
      isbn: isbn.trim() || null,
      publishedYear: Number.isFinite(publishedYear) ? publishedYear : null,
      publisher: null,
      description: null,
      coverImageUrl: null,
      genre: null,
      pageCount: null,
    });
  };

  return (
    <View style={cards.card}>
      <Text style={text.heading}>Add a book by hand</Text>
      <TextField label="Title" value={title} onChangeText={setTitle} maxLength={500} autoFocus />
      <TextField label="Author" value={author} onChangeText={setAuthor} maxLength={500} />
      <TextField label="ISBN (optional)" value={isbn} onChangeText={setIsbn} maxLength={20} keyboardType="numbers-and-punctuation" />
      <TextField label="Year (optional)" value={year} onChangeText={setYear} maxLength={4} keyboardType="number-pad" />
      {error ? <Text style={text.error}>{error}</Text> : null}
      <View style={layout.row}>
        <Button label="Add book" onPress={submit} loading={saving} disabled={!title.trim()} />
        <Button label="Cancel" variant="secondary" onPress={onCancel} disabled={saving} />
      </View>
    </View>
  );
}
