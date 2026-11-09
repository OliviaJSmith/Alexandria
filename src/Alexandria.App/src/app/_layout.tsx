import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import * as WebBrowser from 'expo-web-browser';
import { AuthProvider, useAuth } from '../auth/AuthProvider';
import { LoadingView } from '../components/LoadingView';
import { stackScreenOptions } from '../styles/navigation';

// Closes the Google sign-in popup on web when it redirects back to the app.
WebBrowser.maybeCompleteAuthSession();

export default function RootLayout() {
  return (
    <AuthProvider>
      <StatusBar style="dark" />
      <RootStack />
    </AuthProvider>
  );
}

function RootStack() {
  const { state } = useAuth();

  if (state.status === 'loading') {
    return <LoadingView message="Opening Alexandria…" />;
  }

  const signedIn = state.status === 'signedIn';
  return (
    <Stack screenOptions={stackScreenOptions}>
      <Stack.Protected guard={signedIn}>
        <Stack.Screen name="(tabs)" options={{ headerShown: false, title: 'Libraries' }} />
        <Stack.Screen name="library/[libraryId]/index" options={{ title: 'Library' }} />
        <Stack.Screen name="library/[libraryId]/add-book" options={{ title: 'Add a book' }} />
        <Stack.Screen name="library/[libraryId]/scan" options={{ title: 'Scan books' }} />
        <Stack.Screen name="friend/[friendId]" options={{ title: 'Friend' }} />
        <Stack.Screen name="library/[libraryId]/copy/[copyId]" options={{ title: 'Book' }} />
      </Stack.Protected>
      <Stack.Protected guard={!signedIn}>
        <Stack.Screen name="sign-in" options={{ headerShown: false, title: 'Sign in' }} />
      </Stack.Protected>
    </Stack>
  );
}
