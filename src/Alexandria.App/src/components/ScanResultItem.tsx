import { Text, View } from 'react-native';
import type { ScannedBook } from '../api/types';
import { cards } from '../styles/cards';
import { layout } from '../styles/layout';
import { text } from '../styles/typography';
import { BookCover } from './BookCover';
import { Checkbox } from './Checkbox';

type ScanResultItemProps = {
  scanned: ScannedBook;
  selected: boolean;
  onToggle: (selected: boolean) => void;
};

/** A detected book with a checkbox: shows the catalog match, or what was read when nothing matched. */
export function ScanResultItem({ scanned, selected, onToggle }: ScanResultItemProps) {
  const title = scanned.match?.title ?? scanned.detectedTitle;
  const author = scanned.match?.author ?? scanned.detectedAuthor;

  return (
    <View style={cards.listItem}>
      <Checkbox checked={selected} onChange={onToggle} accessibilityLabel={`Add ${title}`}>
        <View style={layout.row}>
          <BookCover title={title} url={scanned.match?.coverImageUrl ?? null} />
          <View style={layout.grow}>
            <Text style={text.bodyStrong}>{title}</Text>
            {author ? <Text style={text.small}>{author}</Text> : null}
            {!scanned.match ? <Text style={text.small}>No catalog match: it will be added as read from the photo.</Text> : null}
          </View>
        </View>
      </Checkbox>
    </View>
  );
}
