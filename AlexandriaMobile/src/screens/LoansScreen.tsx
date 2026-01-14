import React, { useState, useEffect } from 'react';
import { View, Text, FlatList, StyleSheet, TouchableOpacity } from 'react-native';
import { getLoans } from '../services/api';
import { Loan, LoanStatus } from '../types';

export default function LoansScreen() {
  const [loans, setLoans] = useState<Loan[]>([]);
  const [filter, setFilter] = useState<'all' | 'borrowed' | 'lent'>('all');

  useEffect(() => {
    loadLoans();
  }, [filter]);

  const loadLoans = async () => {
    try {
      const filterParam = filter === 'all' ? undefined : filter;
      const data = await getLoans(filterParam);
      setLoans(data);
    } catch (error) {
      console.error('Load loans error:', error);
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

  const renderLoan = ({ item }: { item: Loan }) => (
    <View style={styles.loanCard}>
      <Text style={styles.bookTitle}>{item.book.title}</Text>
      {item.book.author && <Text style={styles.bookAuthor}>{item.book.author}</Text>}
      
      <View style={styles.loanInfo}>
        <Text style={styles.infoText}>Lender: {item.lenderName}</Text>
        <Text style={styles.infoText}>Borrower: {item.borrowerName}</Text>
        <Text style={styles.infoText}>
          Loan Date: {new Date(item.loanDate).toLocaleDateString()}
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
      
      <FlatList
        data={loans}
        renderItem={renderLoan}
        keyExtractor={(item) => item.id.toString()}
        contentContainerStyle={styles.listContent}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  filterBar: {
    flexDirection: 'row',
    padding: 10,
    backgroundColor: 'white',
    justifyContent: 'space-around',
  },
  filterButton: {
    paddingVertical: 8,
    paddingHorizontal: 20,
    borderRadius: 20,
    backgroundColor: '#f0f0f0',
  },
  filterButtonActive: {
    backgroundColor: '#2196F3',
  },
  filterText: {
    color: '#666',
    fontWeight: '500',
  },
  filterTextActive: {
    color: 'white',
    fontWeight: '500',
  },
  listContent: {
    padding: 15,
  },
  loanCard: {
    backgroundColor: 'white',
    padding: 15,
    borderRadius: 8,
    marginBottom: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  bookTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 5,
  },
  bookAuthor: {
    fontSize: 14,
    color: '#666',
    marginBottom: 10,
  },
  loanInfo: {
    marginBottom: 10,
  },
  infoText: {
    fontSize: 12,
    color: '#666',
    marginBottom: 3,
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
});
