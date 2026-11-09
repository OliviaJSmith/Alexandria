import type { ReactNode } from 'react';
import { Text, View } from 'react-native';
import { feedback } from '../styles/feedback';
import { text } from '../styles/typography';

type EmptyStateProps = {
  title: string;
  message?: string;
  children?: ReactNode;
};

export function EmptyState({ title, message, children }: EmptyStateProps) {
  return (
    <View style={feedback.empty}>
      <Text style={[text.heading, text.center]}>{title}</Text>
      {message ? <Text style={[text.muted, text.center]}>{message}</Text> : null}
      {children}
    </View>
  );
}
