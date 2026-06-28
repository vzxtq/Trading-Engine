import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"
import { Currency } from "@/types/enums"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

/** Visible red for cancel, logout, and other destructive actions (readable on dark backgrounds). */
export const dangerTextClass =
  'text-red-500 dark:text-red-400'

export const dangerActionClass =
  'text-red-500 hover:text-red-600 hover:bg-red-500/10 dark:text-red-400 dark:hover:text-red-300 dark:hover:bg-red-500/10'

export const dangerMenuItemClass =
  'text-red-500 focus:bg-red-500/10 focus:text-red-600 dark:text-red-400 dark:focus:bg-red-500/10 dark:focus:text-red-300'

export const numericClass = 'font-normal text-muted-foreground tabular-nums'

export function formatNumber(value: number, fractionDigits = 2): string {
  if (!Number.isFinite(value)) {
    return (0).toFixed(fractionDigits)
  }
  return value.toFixed(fractionDigits)
}

export const formatAmount = (amount: number, fractionDigits = 2): string =>
  formatNumber(amount, fractionDigits)

export function formatUsd(amount: number, fractionDigits = 2): string {
  return `$${formatNumber(amount, fractionDigits)}`
}

export function formatCurrency(amount: number, currency: Currency, fractionDigits = 2): string {
  const label = Currency[currency]
  const formatted = formatNumber(amount, fractionDigits)
  return label ? `${formatted} ${label}` : formatted
}
