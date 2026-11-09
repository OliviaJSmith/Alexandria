import { Text, View } from 'react-native';
import { statusLabels, type LibraryBookStatus } from '../api/types';
import { feedback } from '../styles/feedback';

const statusStyles = {
  Available: { badge: feedback.badgeAvailable, text: feedback.badgeAvailableText },
  CheckedOut: { badge: feedback.badgeLent, text: feedback.badgeLentText },
  WaitingToBeLoanedOut: { badge: feedback.badgeWaiting, text: feedback.badgeWaitingText },
};

export function StatusBadge({ status }: { status: LibraryBookStatus }) {
  const styles = statusStyles[status];
  return (
    <View style={[feedback.badge, styles.badge]}>
      <Text style={[feedback.badgeText, styles.text]}>{statusLabels[status]}</Text>
    </View>
  );
}
