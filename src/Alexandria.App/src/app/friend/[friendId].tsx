import { router, Stack, useLocalSearchParams } from 'expo-router';
import { useCallback } from 'react';
import { Text, View } from 'react-native';
import { useSignedIn } from '../../auth/AuthProvider';
import { Banner } from '../../components/Banner';
import { EmptyState } from '../../components/EmptyState';
import { ListItem } from '../../components/ListItem';
import { LoadingView } from '../../components/LoadingView';
import { Screen } from '../../components/Screen';
import { useFocusQuery } from '../../hooks/useFocusQuery';
import { layout } from '../../styles/layout';
import { text } from '../../styles/typography';

/** A friend's public libraries. */
export default function FriendScreen() {
  const { friendId, name } = useLocalSearchParams<{ friendId: string; name?: string }>();
  const { api } = useSignedIn();
  const load = useCallback(() => api.getFriendLibraries(friendId), [api, friendId]);
  const libraries = useFocusQuery(load);
  const title = name ? `${name}'s libraries` : 'Libraries';

  if (libraries.loading) {
    return <LoadingView message="Loading libraries…" />;
  }

  return (
    <Screen onRefresh={libraries.reload} refreshing={libraries.refreshing}>
      <Stack.Screen options={{ title }} />
      <View>
        <Text style={text.title}>{title}</Text>
        <Text style={text.muted}>Only libraries they've made public are shown. Open one to ask to borrow a book.</Text>
      </View>
      {libraries.error ? <Banner message={libraries.error} actionLabel="Retry" onAction={libraries.reload} /> : null}
      {libraries.data?.length === 0 ? <EmptyState title="Nothing shared yet" message="They haven't made any libraries public." /> : null}
      <View style={layout.section}>
        {libraries.data?.map((library) => (
          <ListItem
            key={library.id}
            title={library.name}
            onPress={() => router.push({ pathname: '/library/[libraryId]', params: { libraryId: library.id } })}
          />
        ))}
      </View>
    </Screen>
  );
}
