import { useEffect, useState } from 'react';
import { Button, type ButtonVariant } from './Button';

type ConfirmButtonProps = {
  label: string;
  confirmLabel: string;
  onConfirm: () => void;
  variant?: ButtonVariant;
  loading?: boolean;
  disabled?: boolean;
};

/**
 * Two-tap button for destructive actions. Native alert dialogs don't exist on React Native Web,
 * so the first tap arms the button and the second (within a few seconds) confirms.
 */
export function ConfirmButton({ label, confirmLabel, onConfirm, variant = 'danger', loading, disabled }: ConfirmButtonProps) {
  const [armed, setArmed] = useState(false);

  useEffect(() => {
    if (!armed) {
      return;
    }
    const timer = setTimeout(() => setArmed(false), 4000);
    return () => clearTimeout(timer);
  }, [armed]);

  return (
    <Button
      label={armed ? confirmLabel : label}
      variant={variant}
      loading={loading}
      disabled={disabled}
      accessibilityHint={armed ? undefined : 'Tap again to confirm'}
      onPress={() => {
        if (armed) {
          setArmed(false);
          onConfirm();
        } else {
          setArmed(true);
        }
      }}
    />
  );
}
