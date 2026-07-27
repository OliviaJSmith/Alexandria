import React, { useState, useEffect } from 'react';
import { View, Text, FlatList, StyleSheet, TouchableOpacity, Image, Alert, ActivityIndicator } from 'react-native';
import { getLoans, getLentOutBooks, getCurrentUser, updateLoanStatus } from '../services/api';
import { Loan, LoanStatus, LibraryBook, BookStatus } from '../types';

export default function LoansScreen() {
  const [loans, setLoans] = useState<Loan[]>([]);
  const [lentBooks, setLentBooks] = useState<LibraryBook[]>([]);
  const [filter, setFilter] = useState<'all' | 'borrowed' | 'lent'>('all');
  const [currentUserId, setCurrentUserId] = useState<number | null>(null);
  const [updatingId, setUpdatingId] = useState<number | null>(null);

  useEffect(() => {
    getCurrentUser()
      .then((user) => setCurrentUserId(user.id))
      .catch((error) => console.error('Load current user error:', error));
  }, []);

  useEffect(() => {
    loadData();
  }, [filter]);

  const loadData = async () => {
    try {
      if (filter === 'lent') {
        // Load books marked as loaned out
        const books = await getLentOutBooks();
        setLentBooks(books);
        setLoans([]);
      } else {
        // Load formal loan records
        const filterParam = filter === 'all' ? undefined : filter;
        const data = await getLoans(filterParam);
        setLoans(data);
        setLentBooks([]);
      }
    } catch (error) {
      console.error('Load data error:', error);
    }
  };

  const getStatusText = (status: LoanStatus): string => {
    const statusMap = {
      [LoanStatus.Pending]: 'Pending',
      [LoanStatus.Active]: 'Active',
      [LoanStatus.Returned]: 'Returned',
      [LoanStatus.Overdue]: 'Overdue',
      [LoanStatus.Cancelled]: 'Cancelled',
    };
    return statusMap[status] || 'Unknown';
  };

  const getStatusColor = (status: LoanStatus): string => {
    const colorMap = {
      [LoanStatus.Pending]: '#FFA500',
      [LoanStatus.Active]: '#4CAF50',
      [LoanStatus.Returned]: '#2196F3',
      [LoanStatus.Overdue]: '#F44336',
      [LoanStatus.Cancelled]: '#9E9E9E',
    };
    return colorMap[status] || '#000';
  };

  const showAlert = (title: string, message: string) => {
    if (typeof window !== 'undefined' && window.alert) {
      window.alert(`${title}: ${message}`);
    } else {
      Alert.alert(title, message);
    }
  };

  const handleUpdateStatus = async (loan: Loan, status: LoanStatus, successMessage: string) => {
    setUpdatingId(loan.id);
    try {
      await updateLoanStatus(loan.id, status);
      showAlert('Done', successMessage);
      loadData();
    } catch (error: any) {
      console.error('Update loan status error:', error);
      const message =
        typeof error.response?.data === 'string'
          ? error.response.data
          : 'Failed to update loan';
      showAlert('Error', message);
    } finally {
      setUpdatingId(null);
    }
  };

  const renderLoanActions = (item: Loan) => {
    if (currentUserId === null) return null;

    const isLender = item.lenderId === currentUserId;
    const isBorrower = item.borrowerId === currentUserId;
    const busy = updatingId !== null;

    if (updatingId === item.id) {
      return <ActivityIndicator color="#E5A823" size="small" style={styles.actionsRow} />;
    }

    if (item.status === LoanStatus.Pending && isLender) {
      return (
        <View style={styles.actionsRow}>
          <TouchableOpacity
            style={styles.approveButton}
            disabled={busy}
            onPress={() =>
              handleUpdateStatus(item, LoanStatus.Active, `Loan approved for ${item.borrowerName}`)
            }
          >
            <Text style={styles.actionButtonText}>Approve</Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={styles.declineButton}
            disabled={busy}
            onPress={() =>
              handleUpdateStatus(item, LoanStatus.Cancelled, 'Borrow request declined')
            }
          >
            <Text style={styles.declineButtonText}>Decline</Text>
          </TouchableOpacity>
        </View>
      );
    }

    if (item.status === LoanStatus.Pending && isBorrower) {
      return (
        <View style={styles.actionsRow}>
          <TouchableOpacity
            style={styles.declineButton}
            disabled={busy}
            onPress={() =>
              handleUpdateStatus(item, LoanStatus.Cancelled, 'Borrow request cancelled')
            }
          >
            <Text style={styles.declineButtonText}>Cancel Request</Text>
          </TouchableOpacity>
        </View>
      );
    }

    if (item.status === LoanStatus.Active) {
      return (
        <View style={styles.actionsRow}>
          <TouchableOpacity
            style={styles.approveButton}
            disabled={busy}
            onPress={() =>
              handleUpdateStatus(item, LoanStatus.Returned, 'Book marked as returned')
            }
          >
            <Text style={styles.actionButtonText}>Mark Returned</Text>
          </TouchableOpacity>
        </View>
      );
    }

    return null;
  };

  const renderLoan = ({ item }: { item: Loan }) => (
    <View style={styles.loanCard}>
      <Text style={styles.bookTitle}>{item.book.title}</Text>
      {item.book.author && <Text style={styles.bookAuthor}>{item.book.author}</Text>}

      <View style={styles.loanInfo}>
        <Text style={styles.infoText}>Lender: {item.lenderName}</Text>
        <Text style={styles.infoText}>Borrower: {item.borrowerName}</Text>
        <Text style={styles.infoText}>
          {item.status === LoanStatus.Pending ? 'Requested' : 'Loan Date'}: {new Date(item.loanDate).toLocaleDateString()}
        </Text>
        {item.dueDate && (
          <Text style={styles.infoText}>
            Due Date: {new Date(item.dueDate).toLocaleDateString()}
          </Text>
        )}
      </View>

      <View style={[styles.statusBadge, { backgroundColor: getStatusColor(item.status) }]}>
        <Text style={styles.statusText}>{getStatusText(item.status)}</Text>
      </View>

      {renderLoanActions(item)}
    </View>
  );

  const renderLentBook = ({ item }: { item: LibraryBook }) => (
    <View style={styles.loanCard}>
      <View style={styles.bookRow}>
        {item.book.coverImageUrl && (
          <Image source={{ uri: item.book.coverImageUrl }} style={styles.bookCover} />
        )}
        <View style={styles.bookInfo}>
          <Text style={styles.bookTitle}>{item.book.title}</Text>
          {item.book.author && <Text style={styles.bookAuthor}>{item.book.author}</Text>}
          {item.book.genre && <Text style={styles.genreText}>{item.book.genre}</Text>}
        </View>
      </View>
      
      <View style={styles.loanInfo}>
        {item.loanNote ? (
          <Text style={styles.loanNoteText}>Loaned to: {item.loanNote}</Text>
        ) : (
          <Text style={styles.infoText}>Loaned out (no borrower specified)</Text>
        )}
      </View>
      
      <View style={[styles.statusBadge, { backgroundColor: '#FF9800' }]}>
        <Text style={styles.statusText}>Loaned Out</Text>
      </View>
    </View>
  );

  return (
    <View style={styles.container}>
      <View style={styles.filterBar}>
        <TouchableOpacity
          style={[styles.filterButton, filter === 'all' && styles.filterButtonActive]}
          onPress={() => setFilter('all')}
        >
          <Text style={filter === 'all' ? styles.filterTextActive : styles.filterText}>
            All
          </Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.filterButton, filter === 'borrowed' && styles.filterButtonActive]}
          onPress={() => setFilter('borrowed')}
        >
          <Text style={filter === 'borrowed' ? styles.filterTextActive : styles.filterText}>
            Borrowed
          </Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.filterButton, filter === 'lent' && styles.filterButtonActive]}
          onPress={() => setFilter('lent')}
        >
          <Text style={filter === 'lent' ? styles.filterTextActive : styles.filterText}>
            Lent
          </Text>
        </TouchableOpacity>
      </View>
      
      {filter === 'lent' ? (
        <FlatList
          data={lentBooks}
          renderItem={renderLentBook}
          keyExtractor={(item) => `lent-${item.id}`}
          contentContainerStyle={styles.listContent}
          ListEmptyComponent={
            <Text style={styles.emptyText}>No books currently loaned out</Text>
          }
        />
      ) : (
        <FlatList
          data={loans}
          renderItem={renderLoan}
          keyExtractor={(item) => item.id.toString()}
          contentContainerStyle={styles.listContent}
          ListEmptyComponent={
            <Text style={styles.emptyText}>No loans found</Text>
          }
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#121212',
    overflow: 'visible',
  },
  filterBar: {
    flexDirection: 'row',
    padding: 10,
    backgroundColor: '#1E1E1E',
    justifyContent: 'space-around',
  },
  filterButton: {
    paddingVertical: 8,
    paddingHorizontal: 20,
    borderRadius: 20,
    backgroundColor: '#2C2C2C',
  },
  filterButtonActive: {
    backgroundColor: '#E5A823',
  },
  filterText: {
    color: '#888',
    fontWeight: '500',
  },
  filterTextActive: {
    color: '#1A1A1A',
    fontWeight: '500',
  },
  listContent: {
    padding: 15,
  },
  loanCard: {
    backgroundColor: '#1E1E1E',
    padding: 15,
    borderRadius: 8,
    marginBottom: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.3,
    shadowRadius: 4,
    elevation: 3,
  },
  bookRow: {
    flexDirection: 'row',
    marginBottom: 10,
  },
  bookCover: {
    width: 50,
    height: 75,
    borderRadius: 4,
    marginRight: 12,
  },
  bookInfo: {
    flex: 1,
  },
  bookTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#FFFFFF',
    marginBottom: 5,
  },
  bookAuthor: {
    fontSize: 14,
    color: '#B0B0B0',
    marginBottom: 3,
  },
  genreText: {
    fontSize: 12,
    color: '#888',
  },
  loanInfo: {
    marginBottom: 10,
  },
  infoText: {
    fontSize: 12,
    color: '#B0B0B0',
    marginBottom: 3,
  },
  loanNoteText: {
    fontSize: 14,
    color: '#FF9800',
    fontWeight: '500',
  },
  statusBadge: {
    alignSelf: 'flex-start',
    paddingVertical: 4,
    paddingHorizontal: 12,
    borderRadius: 12,
  },
  statusText: {
    color: 'white',
    fontSize: 12,
    fontWeight: '500',
  },
  actionsRow: {
    flexDirection: 'row',
    marginTop: 12,
    gap: 8,
  },
  approveButton: {
    backgroundColor: '#4CAF50',
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 6,
  },
  actionButtonText: {
    color: '#fff',
    fontSize: 14,
    fontWeight: '600',
  },
  declineButton: {
    backgroundColor: '#2C2C2C',
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 6,
    borderWidth: 1,
    borderColor: '#444',
  },
  declineButtonText: {
    color: '#B0B0B0',
    fontSize: 14,
    fontWeight: '500',
  },
  emptyText: {
    color: '#888',
    textAlign: 'center',
    marginTop: 40,
    fontSize: 16,
  },
});
