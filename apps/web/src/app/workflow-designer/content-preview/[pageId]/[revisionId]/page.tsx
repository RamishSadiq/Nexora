import {requireSession} from "@/lib/auth/session";
import {WorkflowContentPreview} from "@/features/workflows/workflow-content-preview";
export default async function PreviewPage({params}:{params:Promise<{pageId:string;revisionId:string}>}){
 const session=await requireSession();if(!session.permissions.includes("content.read"))return <main className="p-8">Content access is required to preview this revision.</main>;
 const {pageId,revisionId}=await params;return <WorkflowContentPreview pageId={pageId} revisionId={revisionId}/>;
}
