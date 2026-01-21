import React from 'react';
import { NavigationContainer, DarkTheme } from '@react-navigation/native';
import { createStackNavigator } from '@react-navigation/stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import LoginScreen from '../screens/LoginScreen';
import BookSearchScreen from '../screens/BookSearchScreen';
import LibrariesScreen from '../screens/LibrariesScreen';
import LoansScreen from '../screens/LoansScreen';
import ImageSearchScreen from '../screens/ImageSearchScreen';

const Stack = createStackNavigator();
const Tab = createBottomTabNavigator();

const CustomDarkTheme = {
  ...DarkTheme,
  colors: {
    ...DarkTheme.colors,
    primary: '#E5A823',
    background: '#121212',
    card: '#1E1E1E',
    text: '#FFFFFF',
    border: '#2C2C2C',
  },
};

function MainTabs() {
  const insets = useSafeAreaInsets();
  
  return (
    <Tab.Navigator
      screenOptions={{
        tabBarActiveTintColor: '#E5A823',
        tabBarInactiveTintColor: '#888',
        tabBarStyle: { 
          backgroundColor: '#1E1E1E', 
          borderTopColor: '#2C2C2C',
          paddingBottom: insets.bottom,
        },
        headerStyle: { backgroundColor: '#1E1E1E' },
        headerTintColor: '#FFFFFF',
        headerShown: true,
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

export default function AppNavigator() {
  return (
    <NavigationContainer theme={CustomDarkTheme}>
      <Stack.Navigator
        initialRouteName="Login"
        screenOptions={{
          headerShown: false,
          headerStyle: { backgroundColor: '#1E1E1E' },
          headerTintColor: '#FFFFFF',
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
            title: 'Search by Image'
          }}
        />
      </Stack.Navigator>
    </NavigationContainer>
  );
}
