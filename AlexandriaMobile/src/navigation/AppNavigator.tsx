import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createStackNavigator } from '@react-navigation/stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import LoginScreen from '../screens/LoginScreen';
import BookSearchScreen from '../screens/BookSearchScreen';
import LibrariesScreen from '../screens/LibrariesScreen';
import LoansScreen from '../screens/LoansScreen';
import ImageSearchScreen from '../screens/ImageSearchScreen';

const Stack = createStackNavigator();
const Tab = createBottomTabNavigator();

function MainTabs() {
  return (
    <Tab.Navigator
      screenOptions={{
        tabBarActiveTintColor: '#2196F3',
        tabBarInactiveTintColor: '#666',
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
    <NavigationContainer>
      <Stack.Navigator
        initialRouteName="Login"
        screenOptions={{
          headerShown: false,
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
