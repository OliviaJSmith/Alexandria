import { Text, View } from 'react-native';
import type { Book } from '../api/types';
import { layout } from '../styles/layout';
import { text } from '../styles/typography';
import { BookCover } from './BookCover';

/** Read-only bibliographic details: cover, title, author and the facts we know. */
export function BookDetails({ book }: { book: Book }) {
  const facts = [
    book.publisher,
    book.publishedYear?.toString(),
    book.pageCount ? `${book.pageCount} pages` : null,
    book.isbn ? `ISBN ${book.isbn}` : null,
  ].filter(Boolean);

  return (
    <View style={layout.section}>
      <View style={layout.row}>
        <BookCover title={book.title} url={book.coverImageUrl} size="large" />
        <View style={layout.grow}>
          <Text style={text.title}>{book.title}</Text>
          {book.author ? <Text style={text.muted}>{book.author}</Text> : null}
          {facts.length > 0 ? <Text style={text.small}>{facts.join(' · ')}</Text> : null}
        </View>
      </View>
      {book.description ? <Text style={text.body}>{book.description}</Text> : null}
    </View>
  );
}
