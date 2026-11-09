import { useState } from 'react';
import { Image, Text, View } from 'react-native';
import { cards } from '../styles/cards';

/** Profile picture, or the name's first letter in a circle. */
export function Avatar({ name, url }: { name: string; url: string | null }) {
  const [failed, setFailed] = useState(false);
  if (!url || failed) {
    return (
      <View style={cards.avatar} accessibilityElementsHidden importantForAccessibility="no">
        <Text style={cards.avatarInitial}>{name.trim().charAt(0).toUpperCase()}</Text>
      </View>
    );
  }
  return <Image source={{ uri: url }} style={cards.avatar} onError={() => setFailed(true)} accessibilityIgnoresInvertColors />;
}
