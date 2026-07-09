export function money(value: number | null | undefined): string {
  return `￥${Number(value ?? 0).toFixed(2)}`;
}

export function dateTime(value: string | null | undefined): string {
  return value ? new Date(value).toLocaleString() : '-';
}

export function minutesText(value: number): string {
  if (value < 60) {
    return `${value} 分钟`;
  }

  const hours = Math.floor(value / 60);
  const minutes = value % 60;
  return minutes === 0 ? `${hours} 小时` : `${hours} 小时 ${minutes} 分钟`;
}
