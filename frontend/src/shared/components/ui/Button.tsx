import type { ReactNode } from 'react';
import { twMerge } from 'tailwind-merge';

interface ButtonProps {
  children: ReactNode;
  variant?: 'primary' | 'outline';
  onClick?: () => void;
  className?: string;
  type?: 'button' | 'submit' | 'reset';
  disabled?: boolean;
}

export function Button({
  children,
  variant = 'primary',
  onClick,
  className,
  type = 'button',
  disabled = false,
}: ButtonProps) {
  const base =
    'px-4 py-2 text-sm rounded-lg font-medium transition-colors cursor-pointer';
  const variants = {
    primary: 'bg-indigo-600 text-white hover:bg-indigo-700',
    outline:
      'border border-gray-300 text-gray-600 hover:border-indigo-600 hover:text-indigo-600',
  };

  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled}
      className={twMerge(
        base,
        variants[variant],
        disabled && 'opacity-60 cursor-not-allowed',
        className,
      )}
    >
      {children}
    </button>
  );
}
