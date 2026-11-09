import { colors, fontSize } from './theme';

/** Header and tab bar options shared by the navigators (React Navigation takes plain style objects here). */
export const stackScreenOptions = {
  headerStyle: { backgroundColor: colors.background },
  headerTintColor: colors.primary,
  headerTitleStyle: { color: colors.text, fontWeight: '600' as const },
  headerShadowVisible: false,
  contentStyle: { backgroundColor: colors.background },
  headerBackButtonDisplayMode: 'minimal' as const,
};

export const tabScreenOptions = {
  headerStyle: { backgroundColor: colors.background },
  headerTitleStyle: { color: colors.text, fontWeight: '600' as const },
  headerShadowVisible: false,
  sceneStyle: { backgroundColor: colors.background },
  tabBarActiveTintColor: colors.primary,
  tabBarInactiveTintColor: colors.textMuted,
  tabBarStyle: { backgroundColor: colors.surface, borderTopColor: colors.border },
  tabBarLabelStyle: { fontSize: fontSize.sm, fontWeight: '600' as const },
  tabBarIconStyle: { display: 'none' as const },
  tabBarLabelPosition: 'beside-icon' as const,
};
