import { Switch, Text, View } from 'react-native';
import { forms } from '../styles/forms';
import { layout } from '../styles/layout';
import { colors } from '../styles/theme';
import { text } from '../styles/typography';

type ToggleRowProps = {
  label: string;
  description?: string;
  value: boolean;
  onChange: (value: boolean) => void;
  disabled?: boolean;
};

export function ToggleRow({ label, description, value, onChange, disabled = false }: ToggleRowProps) {
  return (
    <View style={forms.switchRow}>
      <View style={layout.grow}>
        <Text style={text.bodyStrong}>{label}</Text>
        {description ? <Text style={text.small}>{description}</Text> : null}
      </View>
      <Switch
        value={value}
        onValueChange={onChange}
        disabled={disabled}
        accessibilityLabel={label}
        trackColor={{ true: colors.primary, false: colors.border }}
        thumbColor={colors.surface}
      />
    </View>
  );
}
