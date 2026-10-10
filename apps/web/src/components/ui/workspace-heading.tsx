"use client";
import { useEffect,useState,type ReactNode } from "react";
import { createPortal } from "react-dom";
export function WorkspaceHeading({children,actions}:{children:ReactNode;actions?:ReactNode}){const [target,setTarget]=useState<HTMLElement|null>(null);useEffect(()=>{const frame=requestAnimationFrame(()=>setTarget(document.getElementById("workspace-page-heading")));return()=>cancelAnimationFrame(frame);},[]);const content=<><h1 className="truncate text-lg font-semibold tracking-tight">{children}</h1>{actions}</>;return target?createPortal(content,target):<div className="flex items-center justify-between">{content}</div>;}
