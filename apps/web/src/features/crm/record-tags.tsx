"use client";

import { useEffect, useId, useRef, useState, type FormEvent } from "react";
import { Plus, Tag, X } from "lucide-react";
import type { Item } from "./types";
import styles from "./record-tags.module.css";

type Props = {
  tags: Item[];
  editable: boolean;
  busy: boolean;
  onAdd: (name: string) => Promise<void>;
  onRemove: (tag: Item) => void;
};

function NewTagDialog({ onAdd, onClose }: Pick<Props, "onAdd"> & { onClose: () => void }) {
  const dialog = useRef<HTMLDialogElement>(null);
  const input = useRef<HTMLInputElement>(null);
  const pending = useRef(false);
  const titleId = useId();
  const hintId = useId();
  const errorId = useId();
  const [name, setName] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    const element = dialog.current;
    element?.showModal();
    input.current?.focus();
    return () => { element?.close(); };
  }, []);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending.current) return;
    const value = name.trim();
    if (!value) {
      setError("Enter a tag name.");
      input.current?.focus();
      return;
    }
    pending.current = true;
    setSaving(true);
    setError("");
    try {
      await onAdd(value);
      onClose();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Unable to add this tag. Try again.");
      input.current?.focus();
    } finally {
      pending.current = false;
      setSaving(false);
    }
  }

  return <dialog ref={dialog} className={styles.dialog} aria-labelledby={titleId} aria-describedby={hintId}
    onCancel={event => { event.preventDefault(); if (!pending.current) onClose(); }}>
    <form onSubmit={submit} aria-busy={saving}>
      <header className={styles.dialogHeader}>
        <div className={styles.dialogTitle}><span className={styles.dialogIcon}><Tag size={20} aria-hidden="true" /></span><h2 id={titleId}>New tag</h2></div>
        <button type="button" className={styles.close} aria-label="Close new tag" disabled={saving} onClick={onClose}><X size={18} aria-hidden="true" /></button>
      </header>
      <p id={hintId} className={styles.hint}>Add a tag to help organise this record.</p>
      <label className={styles.label}>Tag name
        <input ref={input} className={styles.input} name="name" value={name} maxLength={80} required readOnly={saving}
          placeholder="e.g. crm pilot" aria-invalid={Boolean(error)} aria-describedby={error ? errorId : undefined}
          onChange={event => { setName(event.target.value); setError(""); }} />
      </label>
      {error && <p id={errorId} role="alert" className={styles.error}>{error}</p>}
      <footer className={styles.actions}>
        <button type="button" className={styles.cancel} disabled={saving} onClick={onClose}>Cancel</button>
        <button type="submit" className={styles.save} disabled={saving}>{saving ? "Adding…" : "Add tag"}</button>
      </footer>
    </form>
  </dialog>;
}

export function RecordTags({ tags, editable, busy, onAdd, onRemove }: Props) {
  const headingId = useId();
  const trigger = useRef<HTMLButtonElement>(null);
  const restoreFocus = useRef(false);
  const [adding, setAdding] = useState(false);
  useEffect(() => {
    if (!adding && restoreFocus.current) {
      restoreFocus.current = false;
      trigger.current?.focus();
    }
  }, [adding]);
  function close() {
    restoreFocus.current = true;
    setAdding(false);
  }
  return <section data-section="Tags" aria-labelledby={headingId} className={styles.tags}>
    <h2 id={headingId}>Tags</h2>
    <div aria-live="polite" aria-relevant="additions removals">
      {tags.length ? <ul className={styles.list}>{tags.map(tag => <li key={tag.id} className={styles.chip}>
        <Tag size={13} className={styles.tagIcon} aria-hidden="true" />
        <span className={styles.name}>{tag.name}</span>
        {editable && <button type="button" className={styles.remove} disabled={busy} aria-label={`Remove ${tag.name}`} title={`Remove ${tag.name}`} onClick={() => onRemove(tag)}><X size={13} aria-hidden="true" /></button>}
      </li>)}</ul> : <p className={styles.empty}>No tags yet.</p>}
    </div>
    {editable && <button ref={trigger} type="button" className={styles.newTag} disabled={busy} aria-haspopup="dialog" onClick={() => setAdding(true)}><Plus size={16} aria-hidden="true" />New tag</button>}
    {adding && editable && <NewTagDialog onAdd={onAdd} onClose={close} />}
  </section>;
}
