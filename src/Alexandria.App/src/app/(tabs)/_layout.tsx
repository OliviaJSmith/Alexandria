import Tabs from 'expo-router/js-tabs';
import { tabScreenOptions } from '../../styles/navigation';

export default function TabsLayout() {
  return (
    <Tabs screenOptions={tabScreenOptions}>
      <Tabs.Screen name="index" options={{ title: 'Libraries' }} />
      <Tabs.Screen name="lent-out" options={{ title: 'Lent out' }} />
      <Tabs.Screen name="profile" options={{ title: 'Profile' }} />
    </Tabs>
  );
}
