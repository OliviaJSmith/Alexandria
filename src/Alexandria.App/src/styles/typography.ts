import { StyleSheet } from 'react-native';
import { colors, fontSize } from './theme';

export const text = StyleSheet.create({
  display: {
    fontSize: fontSize.xxl,
    fontWeight: '700',
    color: colors.text,
  },
  title: {
    fontSize: fontSize.xl,
    fontWeight: '700',
    color: colors.text,
  },
  heading: {
    fontSize: fontSize.lg,
    fontWeight: '600',
    color: colors.text,
  },
  body: {
    fontSize: fontSize.md,
    color: colors.text,
    lineHeight: 22,
  },
  bodyStrong: {
    fontSize: fontSize.md,
    fontWeight: '600',
    color: colors.text,
  },
  muted: {
    fontSize: fontSize.md,
    color: colors.textMuted,
    lineHeight: 22,
  },
  small: {
    fontSize: fontSize.sm,
    color: colors.textMuted,
  },
  label: {
    fontSize: fontSize.sm,
    fontWeight: '600',
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.6,
  },
  error: {
    fontSize: fontSize.sm,
    color: colors.danger,
  },
  center: {
    textAlign: 'center',
  },
});
