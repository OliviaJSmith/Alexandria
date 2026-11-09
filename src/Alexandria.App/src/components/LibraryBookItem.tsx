import type { LibraryBook } from '../api/types';
import { BookCover } from './BookCover';
import { ListItem } from './ListItem';
import { StatusBadge } from './StatusBadge';

type LibraryBookItemProps = {
  copy: LibraryBook;
  onPress?: () => void;
};

/** A copy in a library: cover, title, author and status (plus the loan note when lent out). */
export function LibraryBookItem({ copy, onPress }: LibraryBookItemProps) {
  const subtitle = [copy.book.author, copy.status === 'CheckedOut' ? copy.loanNote : null].filter(Boolean).join(' · ');
  return (
    <ListItem
      title={copy.book.title}
      subtitle={subtitle || null}
      leading={<BookCover title={copy.book.title} url={copy.book.coverImageUrl} />}
      footer={<StatusBadge status={copy.status} />}
      onPress={onPress}
    />
  );
}
