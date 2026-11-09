/** Design tokens. Every style module builds on these; components never hard-code colors or sizes. */

export const colors = {
  background: '#f7f3ec',
  surface: '#ffffff',
  surfaceMuted: '#efe8dc',
  border: '#ddd3c2',
  text: '#2b2520',
  textMuted: '#6f655b',
  textInverse: '#ffffff',
  primary: '#2f5d50',
  primaryPressed: '#244a40',
  primarySoft: '#dfeae5',
  danger: '#a33a2c',
  dangerSoft: '#f6e1dd',
  warning: '#8a5a12',
  warningSoft: '#f6ead3',
  focus: '#2f5d50',
};

export const spacing = {
  xs: 4,
  sm: 8,
  md: 16,
  lg: 24,
  xl: 32,
};

export const radius = {
  sm: 6,
  md: 10,
  lg: 16,
  pill: 999,
};

export const fontSize = {
  sm: 13,
  md: 16,
  lg: 19,
  xl: 24,
  xxl: 30,
};

/** Widest the main column gets on large (web) screens; phones use the full width. */
export const contentMaxWidth = 640;

/** Minimum touch target, per platform accessibility guidance. */
export const touchTarget = 44;
