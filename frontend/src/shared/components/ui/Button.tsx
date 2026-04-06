import type { ReactNode } from 'react';
import { twMerge } from 'tailwind-merge';

interface ButtonProps {
  children: ReactNode;
  variant?: 'primary' | 'outline';
  onClick?: () => void;
  className?: string;
}

export function Button({
  children,
  variant = 'primary',
  onClick,
  className,
}: ButtonProps) {
  const base =
    'px-4 py-2 text-sm rounded-lg font-medium transition-colors cursor-pointer';
  const variants = {
    primary: 'bg-indigo-600 text-white hover:bg-indigo-700',
    outline:
      'border border-gray-300 text-gray-700 hover:border-indigo-600 hover:text-indigo-600',
  };

  return (
    <button
      onClick={onClick}
      className={twMerge(base, variants[variant], className)}
    >
      {children}
    </button>
  );
}
