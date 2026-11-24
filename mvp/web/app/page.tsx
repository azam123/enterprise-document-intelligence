"use client";
import {useState} from "react";
const API=process.env.NEXT_PUBLIC_AI_API_URL??"http://localhost:8000";
export default function Home(){
 const [tenant,setTenant]=useState("demo-company"),[file,setFile]=useState<File|null>(null),[question,setQuestion]=useState(""),[message,setMessage]=useState(""),[answer,setAnswer]=useState(""),[busy,setBusy]=useState(false);
 async function upload(){
  if(!file){setMessage("Choose a PDF, DOCX, TXT or MD file first.");return}
  setBusy(true);setMessage("");
  try{const body=new FormData();body.append("file",file);const r=await fetch(`${API}/documents`,{method:"POST",headers:{"X-Tenant-Id":tenant},body});const d=await r.json();if(!r.ok)throw new Error(d.detail??"Upload failed");setMessage(`Indexed ${d.chunks_indexed} chunks from ${d.filename}.`)}
  catch(e){setMessage(e instanceof Error?e.message:"Upload failed")}finally{setBusy(false)}
 }
 async function ask(){
  if(question.trim().length<3){setMessage("Enter a question of at least 3 characters.");return}
  setBusy(true);setMessage("");setAnswer("");
  try{const r=await fetch(`${API}/ask`,{method:"POST",headers:{"Content-Type":"application/json","X-Tenant-Id":tenant},body:JSON.stringify({question})});const d=await r.json();if(!r.ok)throw new Error(d.detail??"Question failed");setAnswer(d.answer+(d.citations?.length?"\n\nSources:\n"+d.citations.map((c:{filename:string;page:number})=>`• ${c.filename}, page ${c.page}`).join("\n"):""))}
  catch(e){setMessage(e instanceof Error?e.message:"Question failed")}finally{setBusy(false)}
 }
 return <main><h1>Private Enterprise Intelligence</h1><p>Upload company documents and ask questions using a self-hosted AI model. No public GPT or Claude API is used.</p>
 <section className="panel"><h2>Workspace</h2><label htmlFor="tenant">Tenant ID</label><input id="tenant" value={tenant} onChange={e=>setTenant(e.target.value)}/><p className="small">Demo tenant only. Production authentication and tenant provisioning are required before external use.</p></section>
 <section className="panel"><h2>1. Upload documents</h2><input type="file" accept=".pdf,.docx,.txt,.md" onChange={e=>setFile(e.target.files?.[0]??null)}/><button disabled={busy} onClick={upload}>Upload and index</button></section>
 <section className="panel"><h2>2. Ask your documents</h2><input value={question} onChange={e=>setQuestion(e.target.value)} placeholder="What are the contract renewal terms?"/><button disabled={busy} onClick={ask}>Ask privately</button>{answer&&<pre>{answer}</pre>}</section>
 {message&&<section className="panel" role="status">{message}</section>}</main>;
}
