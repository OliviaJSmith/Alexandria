import React, { useState, useEffect, useCallback, useRef } from "react";
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  Alert,
  ActivityIndicator,
  ScrollView,
} from "react-native";
import {
  getCurrentUser,
  updateCurrentUser,
  checkUserNameAvailability,
} from "../services/api";
import { User } from "../types";

export default function ProfileScreen({ navigation }: any) {
  const [user, setUser] = useState<User | null>(null);
  const [userName, setUserName] = useState("");
  const [name, setName] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [userNameAvailable, setUserNameAvailable] = useState<boolean | null>(
    null,
  );
  const [checkingUserName, setCheckingUserName] = useState(false);
  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    loadUser();

    // Cleanup debounce timer on unmount
    return () => {
      if (debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current);
      }
    };
  }, []);

  const loadUser = async () => {
    try {
      const userData = await getCurrentUser();
      setUser(userData);
      setUserName(userData.userName || "");
      setName(userData.name || "");
    } catch (error) {
      console.error("Failed to load user:", error);
      Alert.alert("Error", "Failed to load profile");
    } finally {
      setLoading(false);
    }
  };

  const checkUserName = useCallback(
    async (value: string, currentUser: User | null) => {
      if (!value.trim()) {
        setUserNameAvailable(null);
        setCheckingUserName(false);
        return;
      }

      // Don't check if it's the same as current username
      if (
        currentUser?.userName &&
        value.toLowerCase() === currentUser.userName.toLowerCase()
      ) {
        setUserNameAvailable(true);
        setCheckingUserName(false);
        return;
      }

      setCheckingUserName(true);
      try {
        const available = await checkUserNameAvailability(value);
        setUserNameAvailable(available);
      } catch (error) {
        console.error("Failed to check username:", error);
        setUserNameAvailable(null);
      } finally {
        setCheckingUserName(false);
      }
    },
    [],
  );

  const handleUserNameChange = (value: string) => {
    setUserName(value);

    // Clear existing timer
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }

    // Only check availability if there's a value
    if (value.trim()) {
      setCheckingUserName(true);
      // Set new timer for debounced check
      debounceTimerRef.current = setTimeout(() => {
        checkUserName(value, user);
      }, 500);
    } else {
      setUserNameAvailable(null);
      setCheckingUserName(false);
    }
  };

  const handleSave = async () => {
    if (userNameAvailable === false) {
      Alert.alert("Error", "Username is not available");
      return;
    }

    setSaving(true);
    try {
      const updatedUser = await updateCurrentUser({
        name: name.trim() || undefined,
        userName: userName.trim(),
      });
      setUser(updatedUser);
      setIsEditing(false);
      Alert.alert("Success", "Profile updated successfully");
    } catch (error: any) {
      console.error("Failed to update profile:", error);
      const message = error.response?.data?.error || "Failed to update profile";
      Alert.alert("Error", message);
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    setUserName(user?.userName || "");
    setName(user?.name || "");
    setUserNameAvailable(null);
    setIsEditing(false);
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#E5A823" />
      </View>
    );
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <View style={styles.header}>
        <Text style={styles.title}>Profile</Text>
        {!isEditing && (
          <TouchableOpacity
            style={styles.editButton}
            onPress={() => setIsEditing(true)}
          >
            <Text style={styles.editButtonText}>Edit</Text>
          </TouchableOpacity>
        )}
      </View>

      <View style={styles.infoSection}>
        <Text style={styles.label}>Email</Text>
        <Text style={styles.valueText}>{user?.email}</Text>
      </View>

      <View style={styles.infoSection}>
        <Text style={styles.label}>Display Name</Text>
        {isEditing ? (
          <TextInput
            style={styles.input}
            value={name}
            onChangeText={setName}
            placeholder="Your display name"
            placeholderTextColor="#666"
          />
        ) : (
          <Text style={styles.valueText}>{user?.name || "Not set"}</Text>
        )}
      </View>

      <View style={styles.infoSection}>
        <Text style={styles.label}>Username</Text>
        {isEditing ? (
          <>
            <TextInput
              style={styles.input}
              value={userName}
              onChangeText={handleUserNameChange}
              placeholder="Choose a unique username"
              placeholderTextColor="#666"
              autoCapitalize="none"
              autoCorrect={false}
              autoComplete="off"
              spellCheck={false}
            />
            <View style={styles.userNameStatus}>
              {checkingUserName && (
                <Text style={styles.checkingText}>
                  Checking availability...
                </Text>
              )}
              {!checkingUserName &&
                userNameAvailable === true &&
                userName.trim() && (
                  <Text style={styles.availableText}>
                    ✓ Username is available
                  </Text>
                )}
              {!checkingUserName && userNameAvailable === false && (
                <Text style={styles.unavailableText}>✗ Username is taken</Text>
              )}
            </View>
          </>
        ) : (
          <Text style={styles.valueText}>
            {user?.userName ? `@${user.userName}` : "Not set"}
          </Text>
        )}
      </View>

      {isEditing && (
        <View style={styles.buttonRow}>
          <TouchableOpacity style={styles.cancelButton} onPress={handleCancel}>
            <Text style={styles.cancelButtonText}>Cancel</Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.saveButton, saving && styles.buttonDisabled]}
            onPress={handleSave}
            disabled={saving || userNameAvailable === false}
          >
            {saving ? (
              <ActivityIndicator color="#1A1A1A" />
            ) : (
              <Text style={styles.saveButtonText}>Save</Text>
            )}
          </TouchableOpacity>
        </View>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#121212",
  },
  content: {
    padding: 20,
  },
  loadingContainer: {
    flex: 1,
    backgroundColor: "#121212",
    justifyContent: "center",
    alignItems: "center",
  },
  header: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 30,
  },
  title: {
    fontSize: 28,
    fontWeight: "bold",
    color: "#FFFFFF",
  },
  editButton: {
    paddingVertical: 8,
    paddingHorizontal: 16,
    backgroundColor: "#333",
    borderRadius: 6,
  },
  editButtonText: {
    color: "#E5A823",
    fontSize: 14,
    fontWeight: "600",
  },
  infoSection: {
    marginBottom: 20,
  },
  label: {
    fontSize: 14,
    color: "#888",
    marginBottom: 8,
  },
  valueText: {
    fontSize: 16,
    color: "#FFFFFF",
  },
  input: {
    backgroundColor: "#1E1E1E",
    borderRadius: 8,
    padding: 15,
    fontSize: 16,
    color: "#FFFFFF",
    borderWidth: 1,
    borderColor: "#333",
  },
  userNameStatus: {
    marginTop: 8,
    minHeight: 20,
  },
  checkingText: {
    color: "#888",
    fontSize: 12,
  },
  availableText: {
    color: "#4CAF50",
    fontSize: 12,
  },
  unavailableText: {
    color: "#F44336",
    fontSize: 12,
  },
  buttonRow: {
    flexDirection: "row",
    gap: 12,
    marginTop: 20,
  },
  cancelButton: {
    flex: 1,
    padding: 15,
    borderRadius: 8,
    alignItems: "center",
    borderWidth: 1,
    borderColor: "#666",
  },
  cancelButtonText: {
    color: "#FFFFFF",
    fontSize: 16,
    fontWeight: "600",
  },
  saveButton: {
    flex: 1,
    backgroundColor: "#E5A823",
    padding: 15,
    borderRadius: 8,
    alignItems: "center",
  },
  buttonDisabled: {
    opacity: 0.6,
  },
  saveButtonText: {
    color: "#1A1A1A",
    fontSize: 16,
    fontWeight: "600",
  },
});
