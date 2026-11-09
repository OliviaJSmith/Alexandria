import { Image, Text, View } from 'react-native';
import { useAuth, useSignedIn } from '../../auth/AuthProvider';
import { Button } from '../../components/Button';
import { Screen } from '../../components/Screen';
import { cards } from '../../styles/cards';
import { layout } from '../../styles/layout';
import { text } from '../../styles/typography';

export default function ProfileScreen() {
  const { user } = useSignedIn();
  const { signOut } = useAuth();
  const termsAccepted = user.termsAcceptedAt ? new Date(user.termsAcceptedAt).toLocaleDateString() : null;

  return (
    <Screen>
      <Text style={text.title}>Profile</Text>

      <View style={cards.card}>
        <View style={layout.row}>
          {user.profilePictureUrl ? (
            <Image source={{ uri: user.profilePictureUrl }} style={cards.cover} accessibilityLabel="Profile picture" />
          ) : null}
          <View style={layout.grow}>
            <Text style={text.heading}>{user.displayName}</Text>
            <Text style={text.muted}>@{user.userName}</Text>
            <Text style={text.small}>{user.email}</Text>
          </View>
        </View>
      </View>

      <View style={cards.card}>
        <Text style={text.label}>Beta</Text>
        <Text style={text.body}>
          {termsAccepted ? `You accepted the beta terms on ${termsAccepted}.` : 'You accepted the beta terms when you signed in.'}
        </Text>
      </View>

      <Button label="Sign out" variant="secondary" onPress={() => void signOut()} fullWidth />
    </Screen>
  );
}
