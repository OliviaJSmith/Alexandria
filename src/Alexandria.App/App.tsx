import { StatusBar } from 'expo-status-bar';
import { Text, View } from 'react-native';
import { layout } from './src/styles/layout';

export default function App() {
  return (
    <View style={layout.screen}>
      <Text>Alexandria</Text>
      <StatusBar style="auto" />
    </View>
  );
}
