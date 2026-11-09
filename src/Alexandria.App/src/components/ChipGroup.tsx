import { Pressable, Text, View } from 'react-native';
import { buttons } from '../styles/buttons';
import { layout } from '../styles/layout';

type ChipOption<T extends string> = { value: T; label: string };

type ChipGroupProps<T extends string> = {
  options: ChipOption<T>[];
  value: T | null;
  onChange: (value: T) => void;
  disabled?: boolean;
  accessibilityLabel: string;
};

/** Single-choice set of pill buttons (status picker, library picker). */
export function ChipGroup<T extends string>({ options, value, onChange, disabled = false, accessibilityLabel }: ChipGroupProps<T>) {
  return (
    <View style={layout.rowWrap} accessibilityRole="radiogroup" accessibilityLabel={accessibilityLabel}>
      {options.map((option) => {
        const selected = option.value === value;
        return (
          <Pressable
            key={option.value}
            onPress={() => onChange(option.value)}
            disabled={disabled}
            accessibilityRole="radio"
            accessibilityLabel={option.label}
            accessibilityState={{ checked: selected, disabled }}
            style={[buttons.chip, selected && buttons.chipSelected, disabled && buttons.disabled]}
          >
            <Text style={[buttons.chipLabel, selected && buttons.chipLabelSelected]}>{option.label}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}
