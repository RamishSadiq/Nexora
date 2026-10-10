"use client";
import {useEffect,useState} from "react";
import Link from "next/link";
import {moduleApi} from "@/features/shared/module-api";
import {SiteRenderer} from "@/features/content/site-renderer";
import type {PublicPage} from "@/features/content/types";
export function WorkflowContentPreview({pageId,revisionId}:{pageId:string;revisionId:string}){
 const [content,setContent]=useState<PublicPage|null>(null),[error,setError]=useState("");
 useEffect(()=>{let live=true;void moduleApi<PublicPage>("content",`/pages/${encodeURIComponent(pageId)}/preview/${encodeURIComponent(revisionId)}`).then(value=>{if(live)setContent(value);}).catch(e=>{if(live)setError(e.message);});return()=>{live=false;};},[pageId,revisionId]);
 return <main><div className="flex flex-wrap items-center gap-3 bg-slate-100 p-4 text-slate-900"><h1 className="font-semibold">Reviewed content preview</h1><p className="text-sm">This is the saved revision attached to the approval. Return to the approval inbox to record your decision.</p><Link className="text-blue-700 underline" href="/workflow-designer">Return to WFL Designer</Link></div>{error?<p role="alert" className="p-8 text-red-700">{error}</p>:content?<SiteRenderer preview content={content}/>:<p className="p-8">Loading saved revision…</p>}</main>;
}
