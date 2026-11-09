import type { ReactNode } from 'react';
import { Pressable, Text, View } from 'react-native';
import { forms } from '../styles/forms';
import { layout } from '../styles/layout';

type CheckboxProps = {
  checked: boolean;
  onChange: (checked: boolean) => void;
  accessibilityLabel: string;
  children: ReactNode;
};

export function Checkbox({ checked, onChange, accessibilityLabel, children }: CheckboxProps) {
  return (
    <Pressable
      onPress={() => onChange(!checked)}
      accessibilityRole="checkbox"
      accessibilityLabel={accessibilityLabel}
      accessibilityState={{ checked }}
      style={forms.checkboxRow}
    >
      <View style={[forms.checkbox, checked && forms.checkboxChecked]}>
        {checked ? <Text style={forms.checkmark}>✓</Text> : null}
      </View>
      <View style={layout.grow}>{children}</View>
    </Pressable>
  );
}
