import { useState } from 'react';
import { Image, Text, View } from 'react-native';
import { cards } from '../styles/cards';

type BookCoverProps = {
  title: string;
  url: string | null;
  size?: 'small' | 'large';
};

/** Cover image, or the title's first letter when there is no cover or it fails to load. */
export function BookCover({ title, url, size = 'small' }: BookCoverProps) {
  const [failed, setFailed] = useState(false);
  const frame = size === 'large' ? cards.coverLarge : cards.cover;

  if (!url || failed) {
    return (
      <View style={[frame, cards.coverPlaceholder]} accessibilityLabel={`No cover for ${title}`}>
        <Text style={[cards.coverInitial, size === 'large' && cards.coverInitialLarge]}>
          {title.trim().charAt(0).toUpperCase()}
        </Text>
      </View>
    );
  }

  return (
    <Image
      source={{ uri: url }}
      style={frame}
      resizeMode="cover"
      accessibilityLabel={`Cover of ${title}`}
      onError={() => setFailed(true)}
    />
  );
}
