"use client";

import { isValidElement, type ReactNode } from "react";
import { ArrowDown, ArrowUp, ArrowUpDown } from "lucide-react";
import styles from "./table-sort.module.css";

export type SortValue = string | number | boolean | Date | null | undefined;
export type TableSort = { key: string; direction: "ascending" | "descending" };

export function parseTableSort(value: string): TableSort {
  const canonical = value === "updated" ? "-updatedAtUtc" : value;
  return { key: canonical.replace(/^-/, ""), direction: canonical.startsWith("-") ? "descending" : "ascending" };
}

export function SortableHeader({ label, column, sort, onSort, hidden, disabled = false, className = "p-3" }: {
  label: string; column: string; sort?: TableSort | null; onSort: (value: string) => void;
  hidden?: boolean; disabled?: boolean; className?: string;
}) {
  const active = sort?.key === column;
  const next = active && sort.direction === "ascending" ? "descending" : "ascending";
  const Icon = active ? sort.direction === "ascending" ? ArrowUp : ArrowDown : ArrowUpDown;
  return <th scope="col" hidden={hidden} className={className} aria-label={label} aria-sort={active ? sort.direction : "none"}>
    <button type="button" className={styles.trigger} disabled={disabled} aria-label={`Sort by ${label} ${next}`} title={`Sort ${label} ${next}`} onClick={() => onSort(next === "descending" ? `-${column}` : column)}>
      <span>{label}</span><Icon size={14} aria-hidden="true" className={active ? styles.active : styles.arrow} />
    </button>
  </th>;
}

// Read the display value of existing cells, including links and icon/name pairs.
export function cellSortValue(node: ReactNode): string {
  if (node == null || typeof node === "boolean") return "";
  if (["string", "number", "bigint"].includes(typeof node)) return String(node);
  if (Array.isArray(node)) return node.map(cellSortValue).join("");
  if (isValidElement<{ children?: ReactNode }>(node)) return cellSortValue(node.props.children);
  return "";
}

const collator = new Intl.Collator(undefined, { sensitivity: "base", numeric: true });
export function sortTableRows<T>(rows: T[], sort: TableSort | null, value: (row: T, key: string) => SortValue): T[] {
  if (!sort) return rows;
  return rows.map((row, index) => ({ row, index, value: value(row, sort.key) })).sort((a, b) => {
    const emptyA = a.value == null || a.value === "" || a.value === "—";
    const emptyB = b.value == null || b.value === "" || b.value === "—";
    if (emptyA || emptyB) return emptyA === emptyB ? a.index - b.index : emptyA ? 1 : -1;
    const result = typeof a.value === "number" && typeof b.value === "number" ? a.value - b.value
      : a.value instanceof Date && b.value instanceof Date ? a.value.getTime() - b.value.getTime()
      : collator.compare(String(a.value), String(b.value));
    return (sort.direction === "descending" ? -result : result) || a.index - b.index;
  }).map(item => item.row);
}
