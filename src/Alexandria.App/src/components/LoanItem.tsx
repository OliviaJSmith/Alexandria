import type { ReactNode } from 'react';
import { Text, View } from 'react-native';
import type { Loan } from '../api/types';
import { feedback } from '../styles/feedback';
import { layout } from '../styles/layout';
import { BookCover } from './BookCover';
import { ListItem } from './ListItem';

const shortDate = (iso: string) => new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric' });

type LoanItemProps = {
  loan: Loan;
  /** Whose name to show: the borrower (for the lender's view) or the lender (for the borrower's view). */
  counterpart: 'borrower' | 'lender';
  actions?: ReactNode;
};

export function LoanItem({ loan, counterpart, actions }: LoanItemProps) {
  const person = counterpart === 'borrower' ? loan.borrower : loan.lender;
  const pending = loan.status === 'Pending';
  const relation =
    counterpart === 'borrower'
      ? `${pending ? 'Requested by' : 'Lent to'} ${person.displayName}`
      : `${pending ? 'Asked' : 'From'} ${person.displayName}`;
  const due = loan.dueDate ? `due ${shortDate(loan.dueDate)}` : null;

  return (
    <ListItem
      title={loan.book.title}
      subtitle={[relation, due].filter(Boolean).join(' · ')}
      leading={<BookCover title={loan.book.title} url={loan.book.coverImageUrl} />}
      footer={
        <View style={layout.section}>
          {loan.isOverdue ? (
            <View style={[feedback.badge, feedback.badgeLent]}>
              <Text style={[feedback.badgeText, feedback.badgeLentText]}>Overdue</Text>
            </View>
          ) : null}
          {actions ? <View style={layout.rowWrap}>{actions}</View> : null}
        </View>
      }
    />
  );
}
