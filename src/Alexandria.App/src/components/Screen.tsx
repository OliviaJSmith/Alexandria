import type { ReactNode } from 'react';
import { RefreshControl, ScrollView, View } from 'react-native';
import { layout } from '../styles/layout';
import { colors } from '../styles/theme';

type ScreenProps = {
  children: ReactNode;
  /** Shows pull-to-refresh when provided. */
  onRefresh?: () => void;
  refreshing?: boolean;
};

/** Scrollable page with a centered, width-limited column (full width on phones). */
export function Screen({ children, onRefresh, refreshing = false }: ScreenProps) {
  return (
    <ScrollView
      style={layout.screen}
      contentContainerStyle={layout.scrollContent}
      keyboardShouldPersistTaps="handled"
      refreshControl={
        onRefresh ? <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={colors.primary} /> : undefined
      }
    >
      <View style={layout.column}>{children}</View>
    </ScrollView>
  );
}
