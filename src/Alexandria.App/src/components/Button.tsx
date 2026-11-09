import { ActivityIndicator, Pressable, Text } from 'react-native';
import { buttons } from '../styles/buttons';
import { colors } from '../styles/theme';

export type ButtonVariant = 'primary' | 'secondary' | 'danger';

type ButtonProps = {
  label: string;
  onPress: () => void;
  variant?: ButtonVariant;
  disabled?: boolean;
  loading?: boolean;
  fullWidth?: boolean;
  accessibilityHint?: string;
};

const variantStyles = {
  primary: { idle: buttons.primary, pressed: buttons.primaryPressed, label: buttons.labelPrimary, pressedLabel: buttons.labelPrimary },
  secondary: { idle: buttons.secondary, pressed: buttons.secondaryPressed, label: buttons.labelSecondary, pressedLabel: buttons.labelSecondary },
  danger: { idle: buttons.danger, pressed: buttons.dangerPressed, label: buttons.labelDanger, pressedLabel: buttons.labelDangerPressed },
};

export function Button({
  label,
  onPress,
  variant = 'primary',
  disabled = false,
  loading = false,
  fullWidth = false,
  accessibilityHint,
}: ButtonProps) {
  const styles = variantStyles[variant];
  const inactive = disabled || loading;

  return (
    <Pressable
      onPress={onPress}
      disabled={inactive}
      accessibilityRole="button"
      accessibilityLabel={label}
      accessibilityHint={accessibilityHint}
      accessibilityState={{ disabled: inactive, busy: loading }}
      style={({ pressed }) => [
        buttons.base,
        pressed && !inactive ? styles.pressed : styles.idle,
        inactive && buttons.disabled,
        fullWidth && buttons.fullWidth,
      ]}
    >
      {({ pressed }) => (
        <>
          {loading && <ActivityIndicator color={variant === 'primary' ? colors.textInverse : colors.primary} />}
          <Text style={[buttons.label, pressed && !inactive ? styles.pressedLabel : styles.label]}>{label}</Text>
        </>
      )}
    </Pressable>
  );
}
