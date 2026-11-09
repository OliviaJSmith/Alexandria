import { router } from 'expo-router';
import { Text, View } from 'react-native';
import { useSignedIn } from '../../auth/AuthProvider';
import { Banner } from '../../components/Banner';
import { EmptyState } from '../../components/EmptyState';
import { LibraryBookItem } from '../../components/LibraryBookItem';
import { LoadingView } from '../../components/LoadingView';
import { Screen } from '../../components/Screen';
import { useFocusQuery } from '../../hooks/useFocusQuery';
import { layout } from '../../styles/layout';
import { text } from '../../styles/typography';

export default function LentOutScreen() {
  const { api } = useSignedIn();
  const lentOut = useFocusQuery(api.getLentOutBooks);

  if (lentOut.loading) {
    return <LoadingView message="Loading lent-out books…" />;
  }

  return (
    <Screen onRefresh={lentOut.reload} refreshing={lentOut.refreshing}>
      <View>
        <Text style={text.title}>Lent out</Text>
        <Text style={text.muted}>Books from your libraries that someone else has right now.</Text>
      </View>

      {lentOut.error ? <Banner message={lentOut.error} actionLabel="Retry" onAction={lentOut.reload} /> : null}

      {lentOut.data?.length === 0 ? (
        <EmptyState
          title="Everything is on the shelf"
          message="When you mark a book as lent out, it shows up here so you remember who has it."
        />
      ) : null}

      <View style={layout.section}>
        {lentOut.data?.map((copy) => (
          <LibraryBookItem
            key={copy.id}
            copy={copy}
            onPress={() =>
              router.push({
                pathname: '/library/[libraryId]/copy/[copyId]',
                params: { libraryId: copy.libraryId, copyId: copy.id },
              })
            }
          />
        ))}
      </View>
    </Screen>
  );
}
