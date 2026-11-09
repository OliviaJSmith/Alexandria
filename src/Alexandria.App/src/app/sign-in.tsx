import * as AuthSession from 'expo-auth-session';
import { useEffect, useState } from 'react';
import { Text, View } from 'react-native';
import { errorMessage } from '../api/client';
import { useAuth } from '../auth/AuthProvider';
import { Banner } from '../components/Banner';
import { Button } from '../components/Button';
import { Checkbox } from '../components/Checkbox';
import { Screen } from '../components/Screen';
import { cards } from '../styles/cards';
import { text } from '../styles/typography';
import { betaTerms, config } from '../config';

const googleIssuer = 'https://accounts.google.com';

export default function SignInScreen() {
  const { state, signIn } = useAuth();
  const [acceptedTerms, setAcceptedTerms] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(state.status === 'signedOut' ? state.message ?? null : null);

  // Implicit flow: Google returns an access token directly to the app, so no client secret is involved.
  // The API then verifies the token was issued to Alexandria's client ID.
  const discovery = AuthSession.useAutoDiscovery(googleIssuer);
  const [request, response, promptAsync] = AuthSession.useAuthRequest(
    {
      clientId: config.googleWebClientId,
      scopes: ['openid', 'profile', 'email'],
      responseType: AuthSession.ResponseType.Token,
      usePKCE: false,
      // Fixed path so the URI registered in Google Cloud is exact: <origin>/sign-in on web.
      redirectUri: AuthSession.makeRedirectUri({ path: 'sign-in' }),
    },
    discovery,
  );

  useEffect(() => {
    if (!response) {
      return;
    }
    if (response.type === 'error') {
      setError(response.error?.message ?? 'Google sign-in failed. Please try again.');
      setBusy(false);
      return;
    }
    if (response.type !== 'success') {
      setBusy(false); // dismissed or cancelled
      return;
    }

    const accessToken = response.authentication?.accessToken ?? response.params.access_token;
    if (!accessToken) {
      setError('Google did not return a sign-in token. Please try again.');
      setBusy(false);
      return;
    }

    signIn(accessToken).catch((e) => {
      setError(errorMessage(e));
      setBusy(false);
    });
  }, [response, signIn]);

  const startSignIn = () => {
    setError(null);
    setBusy(true);
    promptAsync().catch(() => {
      setError('Could not open Google sign-in. Please try again.');
      setBusy(false);
    });
  };

  return (
    <Screen>
      <View>
        <Text style={text.display}>Alexandria</Text>
        <Text style={text.muted}>Catalog your home library, share it with friends, and keep track of loans.</Text>
      </View>

      {error ? <Banner message={error} /> : null}

      <View style={cards.card}>
        <Text style={text.heading}>Beta terms</Text>
        <Checkbox
          checked={acceptedTerms}
          onChange={setAcceptedTerms}
          accessibilityLabel="I accept the beta terms"
        >
          <Text style={text.body}>{betaTerms}</Text>
        </Checkbox>
      </View>

      <Button
        label="Sign in with Google"
        onPress={startSignIn}
        disabled={!acceptedTerms || !request}
        loading={busy}
        fullWidth
        accessibilityHint={acceptedTerms ? undefined : 'Accept the beta terms first'}
      />
      {!acceptedTerms ? <Text style={[text.small, text.center]}>Accept the beta terms to continue.</Text> : null}
    </Screen>
  );
}
