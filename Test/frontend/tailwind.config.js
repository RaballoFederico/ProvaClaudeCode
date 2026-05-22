/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './wwwroot/**/*.html',
    './wwwroot/**/*.js'
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        'primary-container': '#e50914',
        'secondary': '#e9c176',
        'background': '#131313',
        'surface': '#131313',
        'surface-container': '#201f1f',
        'surface-container-low': '#1c1b1b',
        'surface-container-high': '#2a2a2a',
        'surface-container-highest': '#333333',
        'on-surface': '#e5e2e1',
        'on-surface-variant': '#e9bcb6',
        'outline-variant': '#5e3f3b',
        'tertiary-container': '#0076c5'
      },
      fontFamily: {
        headline: ['Manrope', 'sans-serif'],
        body: ['Inter', 'sans-serif']
      }
    }
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/container-queries')
  ]
};
