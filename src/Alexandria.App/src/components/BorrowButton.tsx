import { useState } from 'react';
import { ApiError, errorMessage } from '../api/client';
import type { LibraryBook } from '../api/types';
import { useSignedIn } from '../auth/AuthProvider';
import { Button } from './Button';

type BorrowButtonProps = {
  copy: LibraryBook;
  onError: (message: string) => void;
};

/** On a friend's book: asks to borrow it. Becomes "Requested" once sent (or if a request already exists). */
export function BorrowButton({ copy, onError }: BorrowButtonProps) {
  const { api } = useSignedIn();
  const [state, setState] = useState<'idle' | 'sending' | 'requested'>('idle');

  if (copy.status !== 'Available') {
    return null;
  }

  const request = async () => {
    setState('sending');
    try {
      await api.requestToBorrow(copy.id);
      setState('requested');
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) {
        setState('requested');
        return;
      }
      setState('idle');
      onError(errorMessage(e));
    }
  };

  return (
    <Button
      label={state === 'requested' ? 'Requested ✓' : 'Borrow'}
      variant={state === 'requested' ? 'secondary' : 'primary'}
      loading={state === 'sending'}
      disabled={state !== 'idle'}
      onPress={request}
      accessibilityHint={`Asks the owner to lend you ${copy.book.title}`}
    />
  );
}
