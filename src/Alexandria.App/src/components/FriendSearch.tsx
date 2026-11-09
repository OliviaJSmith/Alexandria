import { useState } from 'react';
import { Text, View } from 'react-native';
import { errorMessage } from '../api/client';
import type { UserSearchResult } from '../api/types';
import { useSignedIn } from '../auth/AuthProvider';
import { cards } from '../styles/cards';
import { layout } from '../styles/layout';
import { text } from '../styles/typography';
import { Avatar } from './Avatar';
import { Button } from './Button';
import { ListItem } from './ListItem';
import { TextField } from './TextField';

const relationshipLabels = {
  Friends: 'Friends',
  RequestSent: 'Request sent',
  RequestReceived: 'Wants to be friends',
} as const;

/** Find people by username and send friend requests. Calls onChanged after a request is sent or accepted. */
export function FriendSearch({ onChanged }: { onChanged: () => void }) {
  const { api } = useSignedIn();
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<UserSearchResult[] | null>(null);
  const [searching, setSearching] = useState(false);
  const [sendingTo, setSendingTo] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const search = async () => {
    setSearching(true);
    setError(null);
    try {
      setResults(await api.searchUsers(query.trim()));
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSearching(false);
    }
  };

  const send = async (result: UserSearchResult) => {
    setSendingTo(result.user.id);
    setError(null);
    try {
      const friendship = await api.sendFriendRequest(result.user.userName);
      const relationship = friendship.status === 'Accepted' ? 'Friends' : 'RequestSent';
      setResults((current) =>
        current?.map((r) => (r.user.id === result.user.id ? { ...r, relationship, friendshipId: friendship.friendshipId } : r)) ?? null,
      );
      onChanged();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSendingTo(null);
    }
  };

  return (
    <View style={cards.card}>
      <Text style={text.heading}>Find friends</Text>
      <TextField
        label="Username"
        value={query}
        onChangeText={setQuery}
        placeholder="@CozyQuill42"
        autoCapitalize="none"
        autoCorrect={false}
        returnKeyType="search"
        onSubmitEditing={search}
        maxLength={50}
        error={error}
      />
      <View style={layout.row}>
        <Button label="Search" variant="secondary" onPress={search} loading={searching} disabled={query.trim().replace('@', '').length < 2} />
      </View>
      {results?.length === 0 ? <Text style={text.muted}>Nobody with a username starting “{query.trim()}”.</Text> : null}
      {results?.map((result) => (
        <ListItem
          key={result.user.id}
          title={result.user.displayName}
          subtitle={`@${result.user.userName}`}
          leading={<Avatar name={result.user.displayName} url={result.user.profilePictureUrl} />}
          trailing={
            result.relationship === 'None' || result.relationship === 'RequestReceived' ? (
              <Button
                label={result.relationship === 'RequestReceived' ? 'Accept' : 'Add friend'}
                onPress={() => send(result)}
                loading={sendingTo === result.user.id}
              />
            ) : (
              <Text style={text.small}>{relationshipLabels[result.relationship]}</Text>
            )
          }
        />
      ))}
    </View>
  );
}
