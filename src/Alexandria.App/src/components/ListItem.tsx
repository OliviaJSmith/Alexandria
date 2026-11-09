import type { ReactNode } from 'react';
import { Pressable, Text, View } from 'react-native';
import { cards } from '../styles/cards';
import { text } from '../styles/typography';

type ListItemProps = {
  title: string;
  subtitle?: string | null;
  leading?: ReactNode;
  trailing?: ReactNode;
  /** Extra content under the subtitle, e.g. a status badge. */
  footer?: ReactNode;
  onPress?: () => void;
  accessibilityHint?: string;
};

export function ListItem({ title, subtitle, leading, trailing, footer, onPress, accessibilityHint }: ListItemProps) {
  const body = (
    <>
      {leading}
      <View style={cards.listItemBody}>
        <Text style={text.bodyStrong} numberOfLines={2}>
          {title}
        </Text>
        {subtitle ? (
          <Text style={text.small} numberOfLines={1}>
            {subtitle}
          </Text>
        ) : null}
        {footer}
      </View>
      {trailing}
      {onPress ? <Text style={cards.chevron}>›</Text> : null}
    </>
  );

  if (!onPress) {
    return <View style={cards.listItem}>{body}</View>;
  }

  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      accessibilityLabel={subtitle ? `${title}, ${subtitle}` : title}
      accessibilityHint={accessibilityHint}
      style={({ pressed }) => [cards.listItem, pressed && cards.listItemPressed]}
    >
      {body}
    </Pressable>
  );
}
