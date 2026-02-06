import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  TextInput,
  Alert,
  ActivityIndicator,
  RefreshControl,
  Image,
} from 'react-native';
import {
  getFriends,
  getPendingFriendRequests,
  searchUserByEmail,
  sendFriendRequest,
  acceptFriendRequest,
  removeFriend,
} from '../services/api';
import { Friend, FriendRequest, User } from '../types';

type TabType = 'friends' | 'requests';

export default function FriendsScreen() {
  const [activeTab, setActiveTab] = useState<TabType>('friends');
  const [friends, setFriends] = useState<Friend[]>([]);
  const [pendingRequests, setPendingRequests] = useState<FriendRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  // Search state
  const [searchEmail, setSearchEmail] = useState('');
  const [searchResult, setSearchResult] = useState<User | null>(null);
  const [searching, setSearching] = useState(false);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [sendingRequest, setSendingRequest] = useState(false);

  const loadData = useCallback(async () => {
    try {
      const [friendsData, requestsData] = await Promise.all([
        getFriends(),
        getPendingFriendRequests(),
      ]);
      setFriends(friendsData);
      setPendingRequests(requestsData);
    } catch (error) {
      console.error('Failed to load friends data:', error);
      showAlert('Error', 'Failed to load friends data');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const onRefresh = useCallback(() => {
    setRefreshing(true);
    loadData();
  }, [loadData]);

  const showAlert = (title: string, message: string) => {
    if (typeof window !== 'undefined' && window.alert) {
      window.alert(`${title}: ${message}`);
    } else {
      Alert.alert(title, message);
    }
  };

  const handleSearch = async () => {
    if (!searchEmail.trim()) {
      setSearchError('Please enter an email address');
      return;
    }

    // Basic email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(searchEmail.trim())) {
      setSearchError('Please enter a valid email address');
      return;
    }

    setSearching(true);
    setSearchError(null);
    setSearchResult(null);

    try {
      const user = await searchUserByEmail(searchEmail.trim());
      if (user) {
        // Check if already friends
        const isAlreadyFriend = friends.some((f) => f.friend.id === user.id);
        if (isAlreadyFriend) {
          setSearchError('You are already friends with this user');
          setSearchResult(null);
        } else {
          setSearchResult(user);
        }
      } else {
        setSearchError('No user found with that email address');
      }
    } catch (error) {
      console.error('Search error:', error);
      setSearchError('Failed to search. Please try again.');
    } finally {
      setSearching(false);
    }
  };

  const handleSendRequest = async () => {
    if (!searchResult) return;

    setSendingRequest(true);
    try {
      await sendFriendRequest(searchResult.id);
      showAlert('Success', `Friend request sent to ${searchResult.name}!`);
      setSearchResult(null);
      setSearchEmail('');
    } catch (error: any) {
      console.error('Send request error:', error);
      const message = error.response?.data || 'Failed to send friend request';
      showAlert('Error', message);
    } finally {
      setSendingRequest(false);
    }
  };

  const handleAcceptRequest = async (request: FriendRequest) => {
    try {
      await acceptFriendRequest(request.id);
      showAlert('Success', `You are now friends with ${request.fromUser.name}!`);
      loadData();
    } catch (error) {
      console.error('Accept request error:', error);
      showAlert('Error', 'Failed to accept friend request');
    }
  };

  const handleDeclineRequest = async (request: FriendRequest) => {
    try {
      await removeFriend(request.id);
      showAlert('Declined', 'Friend request declined');
      loadData();
    } catch (error) {
      console.error('Decline request error:', error);
      showAlert('Error', 'Failed to decline friend request');
    }
  };

  const handleRemoveFriend = async (friend: Friend) => {
    const confirmRemove = () => {
      removeFriend(friend.id)
        .then(() => {
          showAlert('Removed', `${friend.friend.name} has been removed from your friends`);
          loadData();
        })
        .catch((error) => {
          console.error('Remove friend error:', error);
          showAlert('Error', 'Failed to remove friend');
        });
    };

    if (typeof window !== 'undefined' && window.confirm) {
      if (window.confirm(`Remove ${friend.friend.name} from your friends?`)) {
        confirmRemove();
      }
    } else {
      Alert.alert(
        'Remove Friend',
        `Are you sure you want to remove ${friend.friend.name} from your friends?`,
        [
          { text: 'Cancel', style: 'cancel' },
          { text: 'Remove', style: 'destructive', onPress: confirmRemove },
        ]
      );
    }
  };

  const renderFriendItem = ({ item }: { item: Friend }) => (
    <View style={styles.friendItem}>
      {item.friend.profilePictureUrl ? (
        <Image source={{ uri: item.friend.profilePictureUrl }} style={styles.avatar} />
      ) : (
        <View style={[styles.avatar, styles.avatarPlaceholder]}>
          <Text style={styles.avatarText}>{item.friend.name.charAt(0).toUpperCase()}</Text>
        </View>
      )}
      <View style={styles.friendInfo}>
        <Text style={styles.friendName}>{item.friend.name}</Text>
        <Text style={styles.friendEmail}>{item.friend.email}</Text>
      </View>
      <TouchableOpacity style={styles.removeButton} onPress={() => handleRemoveFriend(item)}>
        <Text style={styles.removeButtonText}>Remove</Text>
      </TouchableOpacity>
    </View>
  );

  const renderRequestItem = ({ item }: { item: FriendRequest }) => (
    <View style={styles.requestItem}>
      {item.fromUser.profilePictureUrl ? (
        <Image source={{ uri: item.fromUser.profilePictureUrl }} style={styles.avatar} />
      ) : (
        <View style={[styles.avatar, styles.avatarPlaceholder]}>
          <Text style={styles.avatarText}>{item.fromUser.name.charAt(0).toUpperCase()}</Text>
        </View>
      )}
      <View style={styles.friendInfo}>
        <Text style={styles.friendName}>{item.fromUser.name}</Text>
        <Text style={styles.friendEmail}>{item.fromUser.email}</Text>
      </View>
      <View style={styles.requestActions}>
        <TouchableOpacity style={styles.acceptButton} onPress={() => handleAcceptRequest(item)}>
          <Text style={styles.acceptButtonText}>Accept</Text>
        </TouchableOpacity>
        <TouchableOpacity style={styles.declineButton} onPress={() => handleDeclineRequest(item)}>
          <Text style={styles.declineButtonText}>Decline</Text>
        </TouchableOpacity>
      </View>
    </View>
  );

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#4A90A4" />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* Search Section */}
      <View style={styles.searchSection}>
        <Text style={styles.searchTitle}>Add a Friend</Text>
        <Text style={styles.searchSubtitle}>Search by email address to send a friend request</Text>
        <View style={styles.searchInputContainer}>
          <TextInput
            style={styles.searchInput}
            placeholder="Enter email address..."
            placeholderTextColor="#999"
            value={searchEmail}
            onChangeText={(text) => {
              setSearchEmail(text);
              setSearchError(null);
              setSearchResult(null);
            }}
            keyboardType="email-address"
            autoCapitalize="none"
            autoCorrect={false}
          />
          <TouchableOpacity
            style={[styles.searchButton, searching && styles.disabledButton]}
            onPress={handleSearch}
            disabled={searching}
          >
            {searching ? (
              <ActivityIndicator color="#fff" size="small" />
            ) : (
              <Text style={styles.searchButtonText}>Search</Text>
            )}
          </TouchableOpacity>
        </View>

        {searchError && <Text style={styles.errorText}>{searchError}</Text>}

        {searchResult && (
          <View style={styles.searchResultCard}>
            {searchResult.profilePictureUrl ? (
              <Image source={{ uri: searchResult.profilePictureUrl }} style={styles.avatar} />
            ) : (
              <View style={[styles.avatar, styles.avatarPlaceholder]}>
                <Text style={styles.avatarText}>{searchResult.name.charAt(0).toUpperCase()}</Text>
              </View>
            )}
            <View style={styles.searchResultInfo}>
              <Text style={styles.friendName}>{searchResult.name}</Text>
              <Text style={styles.friendEmail}>{searchResult.email}</Text>
            </View>
            <TouchableOpacity
              style={[styles.sendRequestButton, sendingRequest && styles.disabledButton]}
              onPress={handleSendRequest}
              disabled={sendingRequest}
            >
              {sendingRequest ? (
                <ActivityIndicator color="#fff" size="small" />
              ) : (
                <Text style={styles.sendRequestButtonText}>Add Friend</Text>
              )}
            </TouchableOpacity>
          </View>
        )}
      </View>

      {/* Tabs */}
      <View style={styles.tabContainer}>
        <TouchableOpacity
          style={[styles.tab, activeTab === 'friends' && styles.activeTab]}
          onPress={() => setActiveTab('friends')}
        >
          <Text style={[styles.tabText, activeTab === 'friends' && styles.activeTabText]}>
            Friends ({friends.length})
          </Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.tab, activeTab === 'requests' && styles.activeTab]}
          onPress={() => setActiveTab('requests')}
        >
          <Text style={[styles.tabText, activeTab === 'requests' && styles.activeTabText]}>
            Requests ({pendingRequests.length})
          </Text>
          {pendingRequests.length > 0 && (
            <View style={styles.badge}>
              <Text style={styles.badgeText}>{pendingRequests.length}</Text>
            </View>
          )}
        </TouchableOpacity>
      </View>

      {/* Content */}
      {activeTab === 'friends' ? (
        <FlatList
          data={friends}
          renderItem={renderFriendItem}
          keyExtractor={(item) => item.id.toString()}
          contentContainerStyle={styles.listContent}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <Text style={styles.emptyText}>No friends yet</Text>
              <Text style={styles.emptySubtext}>
                Search for friends by their email address above
              </Text>
            </View>
          }
        />
      ) : (
        <FlatList
          data={pendingRequests}
          renderItem={renderRequestItem}
          keyExtractor={(item) => item.id.toString()}
          contentContainerStyle={styles.listContent}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <Text style={styles.emptyText}>No pending requests</Text>
              <Text style={styles.emptySubtext}>Friend requests you receive will appear here</Text>
            </View>
          }
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#f5f5f5',
  },
  searchSection: {
    backgroundColor: '#fff',
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#e0e0e0',
  },
  searchTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#333',
    marginBottom: 4,
  },
  searchSubtitle: {
    fontSize: 14,
    color: '#666',
    marginBottom: 12,
  },
  searchInputContainer: {
    flexDirection: 'row',
    gap: 8,
  },
  searchInput: {
    flex: 1,
    backgroundColor: '#f5f5f5',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 16,
    color: '#333',
    borderWidth: 1,
    borderColor: '#e0e0e0',
  },
  searchButton: {
    backgroundColor: '#4A90A4',
    paddingHorizontal: 20,
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
    minWidth: 80,
  },
  searchButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  disabledButton: {
    opacity: 0.6,
  },
  errorText: {
    color: '#e74c3c',
    fontSize: 14,
    marginTop: 8,
  },
  searchResultCard: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#E3F2FD',
    borderRadius: 8,
    padding: 12,
    marginTop: 12,
    borderWidth: 1,
    borderColor: '#4A90A4',
  },
  searchResultInfo: {
    flex: 1,
    marginLeft: 12,
  },
  sendRequestButton: {
    backgroundColor: '#4CAF50',
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 6,
  },
  sendRequestButtonText: {
    color: '#fff',
    fontSize: 14,
    fontWeight: '600',
  },
  tabContainer: {
    flexDirection: 'row',
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#e0e0e0',
  },
  tab: {
    flex: 1,
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: 14,
    borderBottomWidth: 2,
    borderBottomColor: 'transparent',
  },
  activeTab: {
    borderBottomColor: '#4A90A4',
  },
  tabText: {
    fontSize: 16,
    color: '#666',
    fontWeight: '500',
  },
  activeTabText: {
    color: '#4A90A4',
    fontWeight: '600',
  },
  badge: {
    backgroundColor: '#e74c3c',
    borderRadius: 10,
    minWidth: 20,
    height: 20,
    justifyContent: 'center',
    alignItems: 'center',
    marginLeft: 6,
  },
  badgeText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: '600',
  },
  listContent: {
    padding: 16,
    flexGrow: 1,
  },
  friendItem: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#fff',
    borderRadius: 8,
    padding: 12,
    marginBottom: 8,
    borderWidth: 1,
    borderColor: '#e0e0e0',
  },
  requestItem: {
    backgroundColor: '#fff',
    borderRadius: 8,
    padding: 12,
    marginBottom: 8,
    borderWidth: 1,
    borderColor: '#e0e0e0',
  },
  avatar: {
    width: 48,
    height: 48,
    borderRadius: 24,
  },
  avatarPlaceholder: {
    backgroundColor: '#4A90A4',
    justifyContent: 'center',
    alignItems: 'center',
  },
  avatarText: {
    color: '#fff',
    fontSize: 20,
    fontWeight: '600',
  },
  friendInfo: {
    flex: 1,
    marginLeft: 12,
  },
  friendName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
  },
  friendEmail: {
    fontSize: 14,
    color: '#666',
    marginTop: 2,
  },
  removeButton: {
    backgroundColor: '#f5f5f5',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 6,
    borderWidth: 1,
    borderColor: '#e0e0e0',
  },
  removeButtonText: {
    color: '#e74c3c',
    fontSize: 14,
    fontWeight: '500',
  },
  requestActions: {
    flexDirection: 'row',
    marginTop: 12,
    marginLeft: 60,
    gap: 8,
  },
  acceptButton: {
    backgroundColor: '#4CAF50',
    paddingHorizontal: 20,
    paddingVertical: 8,
    borderRadius: 6,
  },
  acceptButtonText: {
    color: '#fff',
    fontSize: 14,
    fontWeight: '600',
  },
  declineButton: {
    backgroundColor: '#f5f5f5',
    paddingHorizontal: 20,
    paddingVertical: 8,
    borderRadius: 6,
    borderWidth: 1,
    borderColor: '#e0e0e0',
  },
  declineButtonText: {
    color: '#666',
    fontSize: 14,
    fontWeight: '500',
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: 40,
  },
  emptyText: {
    fontSize: 18,
    fontWeight: '600',
    color: '#666',
    marginBottom: 8,
  },
  emptySubtext: {
    fontSize: 14,
    color: '#999',
    textAlign: 'center',
  },
});
