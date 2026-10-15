"use client";
import { WorkspaceHeading } from "@/components/ui/workspace-heading";
import { RecordListing } from "../shared/record-listing";
import listingStyles from "../crm/record-listing.module.css";
import { ArrowLeft, Plus } from "lucide-react";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { moduleApi } from "../shared/module-api";
import { useEntityListing } from "../shared/entity-listing-controls";
import { Field, Panel, buttonClass, inputClass, primaryClass } from "../shared/module-ui";

type User = { id: string; displayName: string; email: string; isActive: boolean; hasPassword: boolean; isProtected: boolean; concurrencyStamp: string; groupIds: string[] };
type Group = { id: string; name: string; description: string; isSystem: boolean; concurrencyStamp: string; permissions: string[]; memberCount: number };
type UserDraft = { displayName: string; email: string; password: string; isActive: boolean; groupIds: string[] };
type GroupDraft = { name: string; description: string; permissions: string[] };
const emptyUser: UserDraft = { displayName: "", email: "", password: "", isActive: true, groupIds: [] };
const emptyGroup: GroupDraft = { name: "", description: "", permissions: [] };
const moduleNames: Record<string, string> = { identity: "Administration", audit: "Audit", crm: "Relationships", membership: "Membership", events: "Events & learning", finance: "Sales & finance", engagement: "Engagement", work: "Work", insights: "Insights", content: "Portal & website", workflows: "Workflow designer" };
const permissionNames: Record<string, string> = { read: "View", manage: "Add and edit", configure: "Configure", files: "Files and evidence", decide: "Application decisions", results: "Exam results", post: "Post invoices", export: "Export", fundraise: "Fundraising", import: "Import", publish: "Publish", create: "Create workflows", edit: "Edit drafts", activate: "Activate/deactivate", delete: "Delete workflows", execute: "Execute", history: "View run history", retry: "Retry failures", cancel: "Cancel runs" };
const api = <T,>(path: string, method = "GET", body?: unknown) => moduleApi<T>("admin", path, method, body);
const groupName = (group: Group) => group.isSystem ? "Admin" : group.name;

export function AccessWorkspace({ section, canManage, currentUserId }: { section: "users" | "security-groups"; canManage: boolean; currentUserId: string }) {
  const router = useRouter();
  const [editorOpen,setEditorOpen]=useState(false);
  const [users, setUsers] = useState<User[]>([]);
  const [groups, setGroups] = useState<Group[]>([]);
  const [permissions, setPermissions] = useState<string[]>([]);
  const [refresh, setRefresh] = useState(0);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [userEdit, setUserEdit] = useState<User | null>(null);
  const [groupEdit, setGroupEdit] = useState<Group | null>(null);
  const [userDraft, setUserDraft] = useState<UserDraft>(emptyUser);
  const [groupDraft, setGroupDraft] = useState<GroupDraft>(emptyGroup);
  const [deleting, setDeleting] = useState<User | Group | null>(null);
  const rows = section === "users" ? users.map(user => ({ ...user, name: user.displayName, description: `${user.email} ${groups.filter(group => user.groupIds.includes(group.id)).map(groupName).join(" ")}`, status: user.isActive ? "active" : "inactive" })) : groups;
  const listing = useEntityListing({ records: rows, columns: section === "users" ? ["Name", "Login name", "Security groups", "Status", "Password"] : ["Name", "Description", "Modules", "Users"] });

  useEffect(() => {
    let active = true;
    Promise.all([api<User[]>("/users"), api<Group[]>("/security-groups"), api<string[]>("/permissions")]).then(([nextUsers, nextGroups, nextPermissions]) => {
      if (active) { setUsers(nextUsers); setGroups(nextGroups); setPermissions(nextPermissions); }
    }).catch(e => { if (active) setError(e.message); }).finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [refresh]);

  async function run(action: () => Promise<void>, message: string) {
    setBusy(true); setError(""); setNotice("");
    try { await action(); setNotice(message); setRefresh(value => value + 1); router.refresh(); }
    catch (e) { setError(e instanceof Error ? e.message : "Unable to save changes."); }
    finally { setBusy(false); }
  }

  function selectPermission(permission: string, checked: boolean) {
    const moduleKey = permission.split(".")[0];
    const read = `${moduleKey}.read`;
    setGroupDraft(draft => {
      let next = draft.permissions.filter(value => value !== permission);
      if (checked) next.push(permission);
      if (checked && permissions.includes(read) && !next.includes(read)) next.push(read);
      if (!checked && permission === read) next = next.filter(value => !value.startsWith(`${moduleKey}.`));
      return { ...draft, permissions: next };
    });
  }

  return <main className={`${!editorOpen?listingStyles.listing:""} mx-auto max-w-7xl space-y-5 p-4 sm:p-7`}>
    <WorkspaceHeading actions={canManage&&!editorOpen?<button type="button" className={listingStyles.create} aria-label={section==="users"?"Add user":"Add security group"} onClick={()=>{setUserEdit(null);setGroupEdit(null);setUserDraft(emptyUser);setGroupDraft(emptyGroup);setEditorOpen(true);}}><Plus size={18}/></button>:undefined}>{section==="users"?"Users":"Security groups"}</WorkspaceHeading>
    {editorOpen&&<button type="button" className={buttonClass} aria-label="Back to listing" onClick={()=>setEditorOpen(false)}><ArrowLeft size={16}/></button>}
    {error && <div role="alert" className="rounded-lg bg-red-50 p-4 text-red-800">{error} <button type="button" className={buttonClass} disabled={busy} onClick={() => { setError(""); setRefresh(value => value + 1); }}>Reload</button></div>}
    {notice && <p role="status" className="text-sm text-green-700">{notice}</p>}
    {!editorOpen&&<>{listing.toolbar}{section==="users"?<RecordListing columnControls={listing.columnControls} label="Users" loading={loading} rows={users.filter(user=>listing.matches(rows.find(row=>row.id===user.id)!))} filterKey={section+listing.filterKey} name={user=>user.displayName} columns={[
      {key:"name",label:"Name",render:user=><>{user.displayName}{user.isProtected&&<span className="ml-2 text-xs text-blue-700">Protected admin</span>}</>},
      {key:"email",label:"Login name",hidden:!listing.show("Login name"),render:user=>user.email},
      {key:"groups",label:"Security groups",hidden:!listing.show("Security groups"),render:user=>groups.filter(group=>user.groupIds.includes(group.id)).map(groupName).join(", ")||"None assigned"},
      {key:"status",label:"Status",hidden:!listing.show("Status"),render:user=>user.isActive?"Active":"Inactive"},
      {key:"password",label:"Password",hidden:!listing.show("Password"),render:user=>user.hasPassword?"Set":"Not set"}
    ]} actions={user=>canManage?<><button disabled={busy} className={buttonClass} aria-label={`Edit ${user.displayName}`} onClick={()=>{setUserEdit(user);setUserDraft({displayName:user.displayName,email:user.email,password:"",isActive:user.isActive,groupIds:user.groupIds});setEditorOpen(true);}}>Edit</button><button className={buttonClass} disabled={busy||user.isProtected||user.id===currentUserId} title={user.isProtected?"Admin accounts cannot be deleted":user.id===currentUserId?"You cannot delete your own account":undefined} aria-label={`Delete ${user.displayName}`} onClick={()=>setDeleting(user)}>Delete</button></>:null}/>:<RecordListing columnControls={listing.columnControls} label="Security groups" loading={loading} rows={groups.filter(listing.matches)} filterKey={section+listing.filterKey} name={groupName} columns={[
      {key:"name",label:"Name",render:group=><>{groupName(group)}{group.isSystem&&<span className="ml-2 text-xs text-blue-700">All access · Protected</span>}</>},
      {key:"description",label:"Description",hidden:!listing.show("Description"),render:group=>group.description},
      {key:"modules",label:"Modules",hidden:!listing.show("Modules"),render:group=>[...new Set(group.permissions.filter(permission=>permission.endsWith(".read")).map(permission=>moduleNames[permission.split(".")[0]]))].join(", ")||"No module access"},
      {key:"users",label:"Users",hidden:!listing.show("Users"),render:group=>group.memberCount}
    ]} actions={group=>canManage?<><button className={buttonClass} disabled={busy||group.isSystem} aria-label={`Edit ${groupName(group)}`} onClick={()=>{setGroupEdit(group);setGroupDraft({name:group.name,description:group.description,permissions:group.permissions});setEditorOpen(true);}}>Edit</button><button className={buttonClass} disabled={busy||group.isSystem||group.memberCount>0} title={group.memberCount>0?"Reassign users before deleting this group":undefined} aria-label={`Delete ${groupName(group)}`} onClick={()=>setDeleting(group)}>Delete</button></>:null}/>}</>}

    {deleting && <Panel title="Confirm deletion"><p className="mb-3 text-sm">Delete {"email" in deleting ? deleting.displayName : deleting.name}? This cannot be undone.</p><div className="flex gap-2"><button disabled={busy} className={primaryClass} onClick={() => void run(async () => { await api(`/${section}/${deleting.id}`, "DELETE", { concurrencyStamp: deleting.concurrencyStamp }); setDeleting(null); if (userEdit?.id === deleting.id) {setUserEdit(null); setUserDraft(emptyUser);} if (groupEdit?.id === deleting.id) {setGroupEdit(null); setGroupDraft(emptyGroup);} }, "Deleted successfully.")}>Confirm delete</button><button disabled={busy} className={buttonClass} onClick={() => setDeleting(null)}>Cancel deletion</button></div></Panel>}

    {editorOpen && canManage && !loading && section === "users" && <Panel title={userEdit ? "Edit user" : "Add user"}><form className="grid gap-4 sm:grid-cols-2" onSubmit={event => { event.preventDefault(); void run(async () => { await api(`/users${userEdit ? `/${userEdit.id}` : ""}`, userEdit ? "PATCH" : "POST", { ...userDraft, password: userDraft.password || null, concurrencyStamp: userEdit?.concurrencyStamp }); setUserEdit(null); setUserDraft(emptyUser); setEditorOpen(false); }, "User saved."); }}>
      <Field label="Display name"><input required maxLength={160} className={inputClass} value={userDraft.displayName} disabled={busy} onChange={event => setUserDraft(draft => ({ ...draft, displayName: event.target.value }))} /></Field>
      <Field label="Login name (email)"><input required type="email" maxLength={256} autoComplete="off" className={inputClass} value={userDraft.email} disabled={busy} onChange={event => setUserDraft(draft => ({ ...draft, email: event.target.value }))} /></Field>
      <Field label={userEdit ? "New password (leave blank to keep current)" : "Password"}><input aria-label={userEdit ? "New password (leave blank to keep current)" : "Password"} type="password" required={!userEdit} minLength={12} maxLength={128} autoComplete="new-password" className={inputClass} value={userDraft.password} disabled={busy} onChange={event => setUserDraft(draft => ({ ...draft, password: event.target.value }))} /><span className="mt-1 block text-xs text-slate-500">At least 12 characters, with uppercase, lowercase, a number and a symbol. Existing passwords are never displayed.</span></Field>
      <Field label="Status"><select className={inputClass} value={String(userDraft.isActive)} disabled={busy || userEdit?.isProtected || userEdit?.id === currentUserId} onChange={event => setUserDraft(draft => ({ ...draft, isActive: event.target.value === "true" }))}><option value="true">Active</option><option value="false">Inactive</option></select></Field>
      <fieldset className="sm:col-span-2"><legend className="mb-2 text-sm font-medium">Security groups</legend><div className="grid gap-2 sm:grid-cols-2">{groups.map(group => <label key={group.id} className="flex gap-3 rounded-lg border border-slate-200 p-3 text-sm"><input type="checkbox" disabled={busy || !!(userEdit?.isProtected && group.isSystem && userEdit.groupIds.includes(group.id))} checked={userDraft.groupIds.includes(group.id)} onChange={event => setUserDraft(draft => ({ ...draft, groupIds: event.target.checked ? [...draft.groupIds, group.id] : draft.groupIds.filter(id => id !== group.id) }))} /><span>{groupName(group)}{group.isSystem && " — all access"}</span></label>)}</div></fieldset>
      <div className="flex gap-2 sm:col-span-2"><button disabled={busy || !userDraft.groupIds.length} className={primaryClass}>Save user</button>{userEdit && <button type="button" disabled={busy} className={buttonClass} onClick={() => { setUserEdit(null); setUserDraft(emptyUser); }}>Cancel edit</button>}</div>
    </form></Panel>}

    {editorOpen && canManage && !loading && section === "security-groups" && <Panel title={groupEdit ? "Edit security group" : "Add security group"}><form className="space-y-4" onSubmit={event => {event.preventDefault(); void run(async () => {await api(`/security-groups${groupEdit ? `/${groupEdit.id}` : ""}`, groupEdit ? "PATCH" : "POST", {...groupDraft, concurrencyStamp: groupEdit?.concurrencyStamp}); setGroupEdit(null); setGroupDraft(emptyGroup);setEditorOpen(false);}, "Security group saved.");}}>
      <Field label="Group name"><input required maxLength={80} className={inputClass} value={groupDraft.name} disabled={busy} onChange={event => setGroupDraft(draft => ({...draft, name: event.target.value}))} /></Field>
      <Field label="Description"><textarea maxLength={300} className={inputClass} value={groupDraft.description} disabled={busy} onChange={event => setGroupDraft(draft => ({...draft, description: event.target.value}))} /></Field>
      <div className="grid gap-3 sm:grid-cols-2">{Object.entries(moduleNames).map(([moduleKey, label]) => <fieldset key={moduleKey} className="rounded-xl border border-slate-200 p-4"><legend className="px-1 text-sm font-semibold">{label}</legend><div className="flex flex-wrap gap-3">{permissions.filter(permission => permission.startsWith(`${moduleKey}.`)).map(permission => <label key={permission} className="flex items-center gap-2 text-sm"><input type="checkbox" checked={groupDraft.permissions.includes(permission)} disabled={busy} onChange={event => selectPermission(permission, event.target.checked)} />{permissionNames[permission.split(".")[1]] ?? permission}</label>)}</div></fieldset>)}</div>
      <div className="flex gap-2"><button disabled={busy} className={primaryClass}>Save security group</button>{groupEdit && <button type="button" disabled={busy} className={buttonClass} onClick={() => {setGroupEdit(null); setGroupDraft(emptyGroup);}}>Cancel edit</button>}</div>
    </form></Panel>}
  </main>;
}
