import { Pressable, Text, View } from 'react-native';
import { feedback } from '../styles/feedback';

type BannerProps = {
  message: string;
  tone?: 'error' | 'info';
  actionLabel?: string;
  onAction?: () => void;
};

export function Banner({ message, tone = 'error', actionLabel, onAction }: BannerProps) {
  return (
    <View
      style={[feedback.banner, tone === 'error' ? feedback.bannerError : feedback.bannerInfo]}
      accessibilityRole={tone === 'error' ? 'alert' : 'text'}
    >
      <Text style={feedback.bannerText}>{message}</Text>
      {actionLabel && onAction ? (
        <Pressable onPress={onAction} accessibilityRole="button" hitSlop={8}>
          <Text style={feedback.bannerAction}>{actionLabel}</Text>
        </Pressable>
      ) : null}
    </View>
  );
}
