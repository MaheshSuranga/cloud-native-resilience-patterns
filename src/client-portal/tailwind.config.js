/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        obsidian: {
          900: '#07090e',
          800: '#0b0f19',
          700: '#111827',
          600: '#1f293d',
        },
        resilience: {
          emerald: '#10b981',
          amber: '#f59e0b',
          rose: '#f43f5e',
          cyan: '#06b6d4',
          indigo: '#6366f1'
        }
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
      animation: {
        'pulse-subtle': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'glow-emerald': 'glowEmerald 2s ease-in-out infinite alternate',
        'glow-amber': 'glowAmber 2s ease-in-out infinite alternate',
      },
      keyframes: {
        glowEmerald: {
          '0%': { boxShadow: '0 0 10px rgba(16, 185, 129, 0.2)' },
          '100%': { boxShadow: '0 0 25px rgba(16, 185, 129, 0.6)' },
        },
        glowAmber: {
          '0%': { boxShadow: '0 0 10px rgba(245, 158, 11, 0.2)' },
          '100%': { boxShadow: '0 0 25px rgba(245, 158, 11, 0.6)' },
        }
      }
    },
  },
  plugins: [],
}
