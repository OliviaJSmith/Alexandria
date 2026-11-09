import { ActivityIndicator, Text, View } from 'react-native';
import { layout } from '../styles/layout';
import { colors } from '../styles/theme';
import { text } from '../styles/typography';

export function LoadingView({ message = 'Loading…' }: { message?: string }) {
  return (
    <View style={[layout.screen, layout.centered]} accessibilityRole="progressbar" accessibilityLabel={message}>
      <ActivityIndicator size="large" color={colors.primary} />
      <Text style={text.muted}>{message}</Text>
    </View>
  );
}
