import { createLightTheme, type BrandVariants } from '@fluentui/react-components';

const qscBrand: BrandVariants = {
  10: '#050818',
  20: '#0e1536',
  30: '#141e54',
  40: '#1a276b',
  50: '#1e2a5e',
  60: '#222e67',
  70: '#263374',
  80: '#2b3b84',
  90: '#3a4d99',
  100: '#4a5fad',
  110: '#5b71c0',
  120: '#6d84d2',
  130: '#8097e3',
  140: '#95abef',
  150: '#b0c3f5',
  160: '#d0dbfa',
};

export const qscTheme = {
  ...createLightTheme(qscBrand),
  fontFamilyBase: "'Roboto', Helvetica, Arial, sans-serif",
  borderRadiusMedium: '0px',
  borderRadiusSmall: '0px',
  borderRadiusLarge: '0px',
  borderRadiusXLarge: '0px',
};
