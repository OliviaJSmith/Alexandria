import React, { useEffect, useState } from 'react';
import { Platform, StyleSheet, View, useWindowDimensions, TouchableOpacity, Text, ActivityIndicator } from 'react-native';
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
import ProfileScreen from '../screens/ProfileScreen';
import { getAuthToken, logout } from '../services/api';

const Stack = createStackNavigator();
const Tab = createBottomTabNavigator();

// Maximum content width for web to prevent overly wide layouts
const MAX_CONTENT_WIDTH = 1200;

function LogoutButton({ navigation }: { navigation: any }) {
  const handleLogout = async () => {
    await logout();
    navigation.reset({
      index: 0,
      routes: [{ name: 'Login' }],
    });
  };

  return (
    <TouchableOpacity style={styles.logoutButton} onPress={handleLogout}>
      <Text style={styles.logoutButtonText}>Logout</Text>
    </TouchableOpacity>
  );
}

function MainTabs({ navigation }: { navigation: any }) {
  return (
    <Tab.Navigator
      screenOptions={{
        tabBarActiveTintColor: '#E5A823',
        tabBarInactiveTintColor: '#666',
        tabBarStyle: {
          backgroundColor: '#1E1E1E',
          borderTopColor: '#333',
          position: 'relative',
          ...(Platform.OS === 'web' && {
            minHeight: 60,
          }),
        },
        headerShown: true,
        headerStyle: {
          backgroundColor: '#1E1E1E',
        },
        headerTintColor: '#FFFFFF',
        headerRight: () => <LogoutButton navigation={navigation} />,
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
      <Tab.Screen
        name="Profile"
        component={ProfileScreen}
        options={{ title: 'Profile' }}
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
  const [isLoading, setIsLoading] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    checkAuthStatus();
  }, []);

  const checkAuthStatus = async () => {
    try {
      const token = await getAuthToken();
      setIsAuthenticated(!!token);
    } catch (error) {
      console.error('Error checking auth status:', error);
      setIsAuthenticated(false);
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return (
      <SafeAreaProvider>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color="#E5A823" />
        </View>
      </SafeAreaProvider>
    );
  }

  return (
    <SafeAreaProvider>
      <SafeAreaView style={styles.safeArea} edges={['top', 'left', 'right']}>
        <ResponsiveContainer>
          <NavigationContainer>
            <Stack.Navigator
              initialRouteName={isAuthenticated ? "Main" : "Login"}
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
                  title: 'Scan a Book',
                  headerStyle: { backgroundColor: '#1E1E1E' },
                  headerTintColor: '#FFFFFF',
                }}
              />
              <Stack.Screen
                name="BookshelfScan"
                component={BookshelfScanScreen}
                options={{
                  headerShown: true,
                  title: 'Scan Bookshelf',
                  headerStyle: { backgroundColor: '#1E1E1E' },
                  headerTintColor: '#FFFFFF',
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
    backgroundColor: '#121212',
  },
  webContainer: {
    flex: 1,
    alignItems: 'center',
    backgroundColor: '#0A0A0A',
  },
  contentContainer: {
    flex: 1,
    width: '100%',
    backgroundColor: '#121212',
    // Add subtle shadow on web for visual separation
    ...(Platform.OS === 'web' && {
      shadowColor: '#000',
      shadowOffset: { width: 0, height: 2 },
      shadowOpacity: 0.1,
      shadowRadius: 8,
    }),
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#121212',
  },
  logoutButton: {
    marginRight: 15,
    paddingVertical: 6,
    paddingHorizontal: 12,
    backgroundColor: '#333',
    borderRadius: 6,
  },
  logoutButtonText: {
    color: '#FFFFFF',
    fontSize: 14,
    fontWeight: '500',
  },
});
