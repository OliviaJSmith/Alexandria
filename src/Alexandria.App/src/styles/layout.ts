import { StyleSheet } from 'react-native';
import { colors, spacing } from './theme';

export const layout = StyleSheet.create({
  screen: {
    flex: 1,
    backgroundColor: colors.background,
    alignItems: 'center',
    justifyContent: 'center',
    padding: spacing.md,
  },
});
