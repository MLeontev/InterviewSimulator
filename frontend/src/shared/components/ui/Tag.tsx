import type { ReactNode } from 'react';
import { twMerge } from 'tailwind-merge';

interface TagProps {
  children: ReactNode;
  className?: string;
}

export function Tag({ children, className }: TagProps) {
  return (
    <span
      className={twMerge(
        'px-3 py-1 rounded-lg bg-indigo-50 text-indigo-700 text-sm',
        className,
      )}
    >
      {children}
    </span>
  );
}
