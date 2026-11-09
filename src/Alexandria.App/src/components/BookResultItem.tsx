import type { Book } from '../api/types';
import { BookCover } from './BookCover';
import { Button } from './Button';
import { ListItem } from './ListItem';

export type AddState = 'idle' | 'adding' | 'added';

type BookResultItemProps = {
  book: Book;
  addState: AddState;
  onAdd: () => void;
};

/** A search result with an Add button. */
export function BookResultItem({ book, addState, onAdd }: BookResultItemProps) {
  const details = [book.author, book.publishedYear?.toString()].filter(Boolean).join(' · ');
  return (
    <ListItem
      title={book.title}
      subtitle={details || null}
      leading={<BookCover title={book.title} url={book.coverImageUrl} />}
      trailing={
        <Button
          label={addState === 'added' ? 'Added ✓' : 'Add'}
          variant={addState === 'added' ? 'secondary' : 'primary'}
          loading={addState === 'adding'}
          disabled={addState !== 'idle'}
          onPress={onAdd}
          accessibilityHint={`Adds ${book.title} to this library`}
        />
      }
    />
  );
}
