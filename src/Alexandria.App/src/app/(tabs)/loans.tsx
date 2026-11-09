import { router } from 'expo-router';
import { useCallback, useState } from 'react';
import { Text, View } from 'react-native';
import { errorMessage } from '../../api/client';
import type { Loan } from '../../api/types';
import { useSignedIn } from '../../auth/AuthProvider';
import { Banner } from '../../components/Banner';
import { Button } from '../../components/Button';
import { EmptyState } from '../../components/EmptyState';
import { LibraryBookItem } from '../../components/LibraryBookItem';
import { LoanItem } from '../../components/LoanItem';
import { LoadingView } from '../../components/LoadingView';
import { Screen } from '../../components/Screen';
import { SectionTitle } from '../../components/SectionTitle';
import { useFocusQuery } from '../../hooks/useFocusQuery';
import { text } from '../../styles/typography';

export default function LoansScreen() {
  const { api, user } = useSignedIn();
  const load = useCallback(async () => {
    const [loans, lentOut] = await Promise.all([api.getLoans('Any'), api.getLentOutBooks()]);
    return { loans, lentOut };
  }, [api]);
  const query = useFocusQuery(load);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  if (query.loading) {
    return <LoadingView message="Loading loans…" />;
  }

  const act = async (loanId: string, action: (id: string) => Promise<unknown>) => {
    setBusyId(loanId);
    setError(null);
    try {
      await action(loanId);
      await query.reload();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setBusyId(null);
    }
  };

  const loans = query.data?.loans ?? [];
  const mine = (loan: Loan) => loan.lender.id === user.id;
  const toApprove = loans.filter((l) => mine(l) && l.status === 'Pending');
  const lent = loans.filter((l) => mine(l) && l.status === 'Active');
  const borrowed = loans.filter((l) => !mine(l) && l.status === 'Active');
  const myRequests = loans.filter((l) => !mine(l) && l.status === 'Pending');
  // Copies marked "lent out" by hand (e.g. to someone not on Alexandria), with no loan tracked here.
  const trackedCopyIds = new Set(lent.map((l) => l.libraryBookId));
  const lentByHand = (query.data?.lentOut ?? []).filter((copy) => !trackedCopyIds.has(copy.id));
  const nothing = toApprove.length + lent.length + borrowed.length + myRequests.length + lentByHand.length === 0;

  const busy = (loan: Loan) => busyId === loan.id;

  return (
    <Screen onRefresh={query.reload} refreshing={query.refreshing}>
      <View>
        <Text style={text.title}>Loans</Text>
        <Text style={text.muted}>Books you've lent, books you've borrowed, and requests waiting on you.</Text>
      </View>
      {query.error ? <Banner message={query.error} actionLabel="Retry" onAction={query.reload} /> : null}
      {error ? <Banner message={error} /> : null}
      {nothing ? <EmptyState title="No loans yet" message="Lend a book from its page, or ask a friend to borrow one from their library." /> : null}

      {toApprove.length > 0 ? <SectionTitle>Requests to borrow your books</SectionTitle> : null}
      {toApprove.map((loan) => (
        <LoanItem
          key={loan.id}
          loan={loan}
          counterpart="borrower"
          actions={
            <>
              <Button label="Approve" onPress={() => act(loan.id, api.approveLoan)} loading={busy(loan)} />
              <Button label="Decline" variant="secondary" onPress={() => act(loan.id, api.declineLoan)} disabled={busy(loan)} />
            </>
          }
        />
      ))}

      {lent.length > 0 ? <SectionTitle>Lent out</SectionTitle> : null}
      {lent.map((loan) => (
        <LoanItem
          key={loan.id}
          loan={loan}
          counterpart="borrower"
          actions={<Button label="Mark returned" variant="secondary" onPress={() => act(loan.id, api.returnLoan)} loading={busy(loan)} />}
        />
      ))}

      {lentByHand.length > 0 ? <SectionTitle>Lent out (tracked by note)</SectionTitle> : null}
      {lentByHand.map((copy) => (
        <LibraryBookItem
          key={copy.id}
          copy={copy}
          onPress={() => router.push({ pathname: '/library/[libraryId]/copy/[copyId]', params: { libraryId: copy.libraryId, copyId: copy.id } })}
        />
      ))}

      {borrowed.length > 0 ? <SectionTitle>Borrowed</SectionTitle> : null}
      {borrowed.map((loan) => (
        <LoanItem key={loan.id} loan={loan} counterpart="lender" />
      ))}

      {myRequests.length > 0 ? <SectionTitle>Your requests</SectionTitle> : null}
      {myRequests.map((loan) => (
        <LoanItem
          key={loan.id}
          loan={loan}
          counterpart="lender"
          actions={<Button label="Cancel request" variant="secondary" onPress={() => act(loan.id, api.cancelLoan)} loading={busy(loan)} />}
        />
      ))}
    </Screen>
  );
}
