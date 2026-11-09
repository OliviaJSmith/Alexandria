import { Text, TextInput, type TextInputProps, View } from 'react-native';
import { forms } from '../styles/forms';
import { colors } from '../styles/theme';
import { text } from '../styles/typography';

type TextFieldProps = Omit<TextInputProps, 'style'> & {
  label: string;
  error?: string | null;
  multiline?: boolean;
};

export function TextField({ label, error, multiline = false, ...inputProps }: TextFieldProps) {
  return (
    <View style={forms.field}>
      <Text style={text.label}>{label}</Text>
      <TextInput
        {...inputProps}
        multiline={multiline}
        accessibilityLabel={label}
        placeholderTextColor={colors.textMuted}
        style={[forms.input, multiline && forms.inputMultiline, error ? forms.inputError : null]}
      />
      {error ? <Text style={text.error}>{error}</Text> : null}
    </View>
  );
}
