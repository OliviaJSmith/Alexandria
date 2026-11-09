import { StyleSheet } from 'react-native';
import { colors, radius, spacing, touchTarget } from './theme';

export const cards = StyleSheet.create({
  card: {
    backgroundColor: colors.surface,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    gap: spacing.sm,
  },
  listItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    minHeight: touchTarget + spacing.md,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
  },
  listItemPressed: {
    backgroundColor: colors.surfaceMuted,
  },
  listItemBody: {
    flex: 1,
    gap: 2,
  },
  chevron: {
    color: colors.textMuted,
    fontSize: 22,
  },
  cover: {
    width: 48,
    height: 72,
    borderRadius: radius.sm,
    backgroundColor: colors.surfaceMuted,
  },
  coverLarge: {
    width: 120,
    height: 180,
    borderRadius: radius.md,
    backgroundColor: colors.surfaceMuted,
  },
  coverPlaceholder: {
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.border,
  },
  coverInitial: {
    color: colors.textMuted,
    fontWeight: '700',
    fontSize: 20,
  },
  coverInitialLarge: {
    fontSize: 44,
  },
});
