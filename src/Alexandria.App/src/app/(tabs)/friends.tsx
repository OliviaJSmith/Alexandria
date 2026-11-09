import { router } from 'expo-router';
import { useCallback, useEffect, useState } from 'react';
import { Text, View } from 'react-native';
import { errorMessage } from '../../api/client';
import type { Friendship } from '../../api/types';
import { useSignedIn } from '../../auth/AuthProvider';
import { Avatar } from '../../components/Avatar';
import { Banner } from '../../components/Banner';
import { Button } from '../../components/Button';
import { ConfirmButton } from '../../components/ConfirmButton';
import { EmptyState } from '../../components/EmptyState';
import { FriendSearch } from '../../components/FriendSearch';
import { ListItem } from '../../components/ListItem';
import { LoadingView } from '../../components/LoadingView';
import { Screen } from '../../components/Screen';
import { SectionTitle } from '../../components/SectionTitle';
import { TextField } from '../../components/TextField';
import { useFocusQuery } from '../../hooks/useFocusQuery';
import { layout } from '../../styles/layout';
import { text } from '../../styles/typography';

export default function FriendsScreen() {
  const { api } = useSignedIn();
  const load = useCallback(async () => {
    const [friends, requests] = await Promise.all([api.getFriends(), api.getFriendRequests()]);
    return { friends, requests };
  }, [api]);
  const query = useFocusQuery(load);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState('');
  const [matches, setMatches] = useState<Friendship[] | null>(null);

  // Search your friends by username or real name (debounced; a blank box shows everyone).
  const term = filter.trim();
  useEffect(() => {
    if (!term) {
      setMatches(null);
      return;
    }
    let cancelled = false;
    const timer = setTimeout(async () => {
      try {
        const found = await api.getFriends(term);
        if (!cancelled) {
          setMatches(found);
        }
      } catch (e) {
        if (!cancelled) {
          setError(errorMessage(e));
        }
      }
    }, 300);
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [api, term]);

  if (query.loading) {
    return <LoadingView message="Loading friends…" />;
  }

  const act = async (friendshipId: string, action: (id: string) => Promise<unknown>) => {
    setBusyId(friendshipId);
    setError(null);
    try {
      await action(friendshipId);
      await query.reload();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setBusyId(null);
    }
  };

  const person = (friendship: Friendship) => ({
    title: friendship.user.displayName,
    subtitle: `@${friendship.user.userName}`,
    leading: <Avatar name={friendship.user.displayName} url={friendship.user.profilePictureUrl} />,
  });

  const incoming = query.data?.requests.incoming ?? [];
  const outgoing = query.data?.requests.outgoing ?? [];
  const friends = query.data?.friends ?? [];
  // Keep only matches that are still friends, so an unfriend during a search disappears at once.
  const shown = matches ? matches.filter((m) => friends.some((f) => f.friendshipId === m.friendshipId)) : friends;

  return (
    <Screen onRefresh={query.reload} refreshing={query.refreshing}>
      <View>
        <Text style={text.title}>Friends</Text>
        <Text style={text.muted}>Friends can see your public libraries and ask to borrow books.</Text>
      </View>
      {query.error ? <Banner message={query.error} actionLabel="Retry" onAction={query.reload} /> : null}
      {error ? <Banner message={error} /> : null}

      <FriendSearch onChanged={() => void query.reload()} />

      {incoming.length > 0 ? <SectionTitle>Friend requests</SectionTitle> : null}
      {incoming.map((request) => (
        <ListItem
          key={request.friendshipId}
          {...person(request)}
          footer={
            <View style={layout.row}>
              <Button label="Accept" onPress={() => act(request.friendshipId, api.acceptFriendRequest)} loading={busyId === request.friendshipId} />
              <Button label="Decline" variant="secondary" onPress={() => act(request.friendshipId, api.declineFriendRequest)} disabled={busyId === request.friendshipId} />
            </View>
          }
        />
      ))}

      {outgoing.length > 0 ? <SectionTitle>Waiting for a reply</SectionTitle> : null}
      {outgoing.map((request) => (
        <ListItem
          key={request.friendshipId}
          {...person(request)}
          trailing={
            <Button label="Cancel" variant="secondary" onPress={() => act(request.friendshipId, api.removeFriendship)} loading={busyId === request.friendshipId} />
          }
        />
      ))}

      <SectionTitle>Your friends</SectionTitle>
      {friends.length === 0 ? (
        <EmptyState title="No friends yet" message="Search for a username above to send a friend request." />
      ) : (
        <TextField
          label="Search your friends"
          value={filter}
          onChangeText={setFilter}
          placeholder="Username or name"
          autoCapitalize="none"
          autoCorrect={false}
          maxLength={100}
        />
      )}
      {matches && shown.length === 0 ? <EmptyState title="No matches" message={`None of your friends match "${term}".`} /> : null}
      {shown.map((friendship) => (
        <ListItem
          key={friendship.friendshipId}
          {...person(friendship)}
          onPress={() =>
            router.push({ pathname: '/friend/[friendId]', params: { friendId: friendship.user.id, name: friendship.user.displayName } })
          }
          footer={
            <View style={layout.row}>
              <ConfirmButton
                label="Unfriend"
                confirmLabel="Tap again to unfriend"
                variant="secondary"
                loading={busyId === friendship.friendshipId}
                onConfirm={() => act(friendship.friendshipId, api.removeFriendship)}
              />
            </View>
          }
        />
      ))}
    </Screen>
  );
}
