import { useState } from 'react';
import { Text, View } from 'react-native';
import { errorMessage } from '../api/client';
import type { Friendship, LibraryBook, Loan } from '../api/types';
import { useSignedIn } from '../auth/AuthProvider';
import { cards } from '../styles/cards';
import { layout } from '../styles/layout';
import { text } from '../styles/typography';
import { Button } from './Button';
import { ChipGroup } from './ChipGroup';

type DuePreset = 'none' | 'twoWeeks' | 'month';

const dueOptions: { value: DuePreset; label: string }[] = [
  { value: 'none', label: 'No due date' },
  { value: 'twoWeeks', label: '2 weeks' },
  { value: 'month', label: '1 month' },
];

function dueDateFor(preset: DuePreset): string | null {
  const days = preset === 'twoWeeks' ? 14 : preset === 'month' ? 30 : 0;
  return days ? new Date(Date.now() + days * 24 * 60 * 60 * 1000).toISOString() : null;
}

type LendCardProps = {
  copy: LibraryBook;
  /** The active loan for this copy, if it's out through Alexandria. */
  activeLoan: Loan | null;
  friends: Friendship[];
  onChanged: () => Promise<void>;
};

/** Owner-only: lend the copy to a friend, or mark an active loan returned. */
export function LendCard({ copy, activeLoan, friends, onChanged }: LendCardProps) {
  const { api } = useSignedIn();
  const [borrowerId, setBorrowerId] = useState<string | null>(null);
  const [due, setDue] = useState<DuePreset>('twoWeeks');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const run = async (work: () => Promise<unknown>) => {
    setBusy(true);
    setError(null);
    try {
      await work();
      await onChanged();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setBusy(false);
    }
  };

  if (activeLoan) {
    const dueText = activeLoan.dueDate ? `, due ${new Date(activeLoan.dueDate).toLocaleDateString()}` : '';
    return (
      <View style={cards.card}>
        <Text style={text.label}>On loan</Text>
        <Text style={text.body}>
          Lent to {activeLoan.borrower.displayName}{dueText}.{activeLoan.isOverdue ? ' It is overdue.' : ''}
        </Text>
        {error ? <Text style={text.error}>{error}</Text> : null}
        <View style={layout.row}>
          <Button label="Mark returned" variant="secondary" loading={busy} onPress={() => run(() => api.returnLoan(activeLoan.id))} />
        </View>
      </View>
    );
  }

  const canLend = copy.status === 'Available' || copy.status === 'WaitingToBeLoanedOut';
  return (
    <View style={cards.card}>
      <Text style={text.label}>Lend to a friend</Text>
      {friends.length === 0 ? <Text style={text.small}>Add friends from the Friends tab to lend them books.</Text> : null}
      {!canLend ? <Text style={text.small}>Mark the book as on the shelf before lending it.</Text> : null}
      {friends.length > 0 && canLend ? (
        <>
          <ChipGroup
            accessibilityLabel="Friend"
            options={friends.map((f) => ({ value: f.user.id, label: f.user.displayName }))}
            value={borrowerId}
            onChange={setBorrowerId}
            disabled={busy}
          />
          <ChipGroup accessibilityLabel="Due date" options={dueOptions} value={due} onChange={setDue} disabled={busy} />
          {error ? <Text style={text.error}>{error}</Text> : null}
          <View style={layout.row}>
            <Button
              label="Lend"
              loading={busy}
              disabled={!borrowerId}
              onPress={() => borrowerId && run(() => api.lendBook(copy.id, borrowerId, dueDateFor(due)))}
            />
          </View>
        </>
      ) : null}
    </View>
  );
}
