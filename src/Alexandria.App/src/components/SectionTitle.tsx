import { Text } from 'react-native';
import { cards } from '../styles/cards';
import { text } from '../styles/typography';

export function SectionTitle({ children }: { children: string }) {
  return (
    <Text style={[text.label, cards.sectionTitle]} accessibilityRole="header">
      {children}
    </Text>
  );
}
