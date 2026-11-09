import { StyleSheet } from 'react-native';
import { colors, fontSize, radius, spacing } from './theme';

/** Banners, badges, empty and loading states. */
export const feedback = StyleSheet.create({
  banner: {
    borderRadius: radius.md,
    borderWidth: 1,
    padding: spacing.md,
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: spacing.sm,
  },
  bannerError: {
    backgroundColor: colors.dangerSoft,
    borderColor: colors.danger,
  },
  bannerInfo: {
    backgroundColor: colors.primarySoft,
    borderColor: colors.primary,
  },
  bannerText: {
    flex: 1,
    fontSize: fontSize.md,
    color: colors.text,
  },
  bannerAction: {
    fontSize: fontSize.md,
    fontWeight: '600',
    color: colors.primary,
  },
  badge: {
    alignSelf: 'flex-start',
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: radius.pill,
  },
  badgeText: {
    fontSize: fontSize.sm,
    fontWeight: '600',
  },
  badgeAvailable: {
    backgroundColor: colors.primarySoft,
  },
  badgeAvailableText: {
    color: colors.primary,
  },
  badgeLent: {
    backgroundColor: colors.warningSoft,
  },
  badgeLentText: {
    color: colors.warning,
  },
  badgeWaiting: {
    backgroundColor: colors.surfaceMuted,
  },
  badgeWaitingText: {
    color: colors.textMuted,
  },
  empty: {
    alignItems: 'center',
    gap: spacing.sm,
    paddingVertical: spacing.xl,
    paddingHorizontal: spacing.md,
  },
});
