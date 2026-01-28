import React from 'react';
import { Platform, StyleSheet, View, useWindowDimensions } from 'react-native';
import { NavigationContainer } from '@react-navigation/native';
import { createStackNavigator } from '@react-navigation/stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';
import LoginScreen from '../screens/LoginScreen';
import BookSearchScreen from '../screens/BookSearchScreen';
import LibrariesScreen from '../screens/LibrariesScreen';
import LoansScreen from '../screens/LoansScreen';
import ImageSearchScreen from '../screens/ImageSearchScreen';
import BookshelfScanScreen from '../screens/BookshelfScanScreen';

const Stack = createStackNavigator();
const Tab = createBottomTabNavigator();

// Maximum content width for web to prevent overly wide layouts
const MAX_CONTENT_WIDTH = 1200;

function MainTabs() {
  return (
    <Tab.Navigator
      screenOptions={{
        tabBarActiveTintColor: '#2196F3',
        tabBarInactiveTintColor: '#666',
        headerShown: true,
        // Ensure tab bar is always visible and doesn't get cut off
        tabBarStyle: {
          position: 'relative',
          ...(Platform.OS === 'web' && {
            minHeight: 60,
          }),
        },
      }}
    >
      <Tab.Screen
        name="Search"
        component={BookSearchScreen}
        options={{ title: 'Search Books' }}
      />
      <Tab.Screen
        name="Libraries"
        component={LibrariesScreen}
        options={{ title: 'My Libraries' }}
      />
      <Tab.Screen
        name="Loans"
        component={LoansScreen}
        options={{ title: 'Loans' }}
      />
    </Tab.Navigator>
  );
}

function ResponsiveContainer({ children }: { children: React.ReactNode }) {
  const { width } = useWindowDimensions();

  // On web with wide viewports, center content with max width
  if (Platform.OS === 'web' && width > MAX_CONTENT_WIDTH) {
    return (
      <View style={styles.webContainer}>
        <View style={[styles.contentContainer, { maxWidth: MAX_CONTENT_WIDTH }]}>
          {children}
        </View>
      </View>
    );
  }

  return <>{children}</>;
}

export default function AppNavigator() {
  return (
    <SafeAreaProvider>
      <SafeAreaView style={styles.safeArea} edges={['top', 'left', 'right']}>
        <ResponsiveContainer>
          <NavigationContainer>
            <Stack.Navigator
              initialRouteName="Login"
              screenOptions={{
                headerShown: false,
                // Ensure proper card styling for web
                ...(Platform.OS === 'web' && {
                  cardStyle: { flex: 1 },
                }),
              }}
            >
              <Stack.Screen
                name="Login"
                component={LoginScreen}
              />
              <Stack.Screen
                name="Main"
                component={MainTabs}
              />
              <Stack.Screen
                name="ImageSearch"
                component={ImageSearchScreen}
                options={{
                  headerShown: true,
                  title: 'Scan a Book'
                }}
              />
              <Stack.Screen
                name="BookshelfScan"
                component={BookshelfScanScreen}
                options={{
                  headerShown: true,
                  title: 'Scan Bookshelf'
                }}
              />
            </Stack.Navigator>
          </NavigationContainer>
        </ResponsiveContainer>
      </SafeAreaView>
    </SafeAreaProvider>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#fff',
  },
  webContainer: {
    flex: 1,
    alignItems: 'center',
    backgroundColor: '#f5f5f5',
  },
  contentContainer: {
    flex: 1,
    width: '100%',
    backgroundColor: '#fff',
    // Add subtle shadow on web for visual separation
    ...(Platform.OS === 'web' && {
      shadowColor: '#000',
      shadowOffset: { width: 0, height: 2 },
      shadowOpacity: 0.1,
      shadowRadius: 8,
    }),
  },
});
