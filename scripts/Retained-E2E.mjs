// Local-only, retained review data. Never targets production or deletes records.
import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
import { randomUUID } from 'node:crypto';
const root = path.resolve(import.meta.dirname, '..');
const require = createRequire(path.join(root, 'apps/web/package.json'));
export const { request, chromium, expect } = require('@playwright/test');
const launch = JSON.parse(fs.readFileSync(path.join(root, '.vscode/launch.json'), 'utf8'));
const config = launch.configurations.find(c => c.name === 'Nexora shared API');
if (!config?.env.ConnectionStrings__Nexora.includes('Database=NexoraDev;') || !config.args.includes('http://localhost:5097')) throw Error('Restricted to configured local NexoraDev.');
export const password = config.env.Identity__SeedAdminPassword;
export const prefix = 'Journey 20261001';
export const output = path.join(root, 'artifacts/retained-e2e-20261001');
fs.mkdirSync(output, { recursive: true });
const ledgerPath = path.join(output, 'records.json');
export const ledger = fs.existsSync(ledgerPath) ? JSON.parse(fs.readFileSync(ledgerPath, 'utf8')) : { prefix, startedAt: new Date().toISOString(), records: {}, checks: [], failures: [] };
export function save() { fs.writeFileSync(ledgerPath, JSON.stringify(ledger, null, 2)); }
export async function retained(key, work) {
  if (ledger.records[key]) return ledger.records[key];
  const result = await work();
  ledger.records[key] = result ?? { completed: true };
  save();
  return ledger.records[key];
}
export function check(name, detail) { ledger.checks.push({ name, detail, at: new Date().toISOString() }); save(); console.log(`PASS ${name}`); }
export async function connect() {
  const api = await request.newContext({ baseURL: 'http://localhost:5097', timeout: 60000 });
  const get = async url => { const r = await api.get(url); if (!r.ok()) throw Error(`GET ${url}: ${r.status()} ${(await r.text()).slice(0,700)}`); return r.json(); };
  const send = async (url, data, method = 'POST') => {
    const { token } = await get('/api/v1/auth/csrf');
    const r = await api.fetch(url, { method, data, headers: { 'X-CSRF-TOKEN': token } });
    if (!r.ok()) throw Error(`${method} ${url}: ${r.status()} ${(await r.text()).slice(0,1000)}`);
    return r.status() === 204 ? null : r.json();
  };
  await send('/api/v1/auth/login', { email: 'admin@nexora.local', password, rememberMe: false });
  return { api, get, send };
}
export const people = [
  ['Amelia','Clarke','Bristol','BS1 4QA','Governance','Director'], ['Oliver','Bennett','Leeds','LS1 2AB','Finance','Finance Manager'],
  ['Isla','Patel','Manchester','M1 3AD','Membership','Membership Officer'], ['Noah','Williams','Cardiff','CF10 1AA','Learning','Training Lead'],
  ['Ava','Khan','Birmingham','B1 1AA','Operations','Operations Manager'], ['Leo','Evans','Edinburgh','EH1 1BB','Policy','Policy Adviser'],
  ['Mia','Thompson','Liverpool','L1 8JQ','Communications','Communications Lead'], ['Arthur','Wilson','Nottingham','NG1 2AS','Quality','Quality Manager'],
  ['Freya','Roberts','Sheffield','S1 2HE','Sustainability','Sustainability Lead'], ['Oscar','Lewis','Cambridge','CB2 1TN','Research','Research Fellow'],
];
export const topics = ['Governance Essentials','Financial Stewardship','Member Experience','Learning Design','Operational Excellence','Policy Forum','Community Communications','Quality Improvement','Sustainable Practice','Research Exchange'];
const date = (i, hour = 10) => new Date(Date.UTC(2026, 10, 2 + i, hour)).toISOString();

async function main() {
  const { api, get, send } = await connect();
  const phase = process.argv[2] ?? 'inventory';
  try {
    if (phase === 'inventory') {
      for (const url of ['/api/v1/crm/records?pageSize=100','/api/v1/membership/products','/api/v1/events/offerings','/api/v1/finance/products','/api/v1/work/items','/api/v1/content/site','/api/v1/admin/users','/api/v1/admin/security-groups','/api/v1/membership/setup/member-type','/api/v1/workflow/definitions']) {
        const r = await api.get(url); const body = r.ok() ? await r.json() : null;
        const summary = { url, status: r.status(), count: Array.isArray(body) ? body.length : body?.totalCount ?? body?.pages?.length, keys: body && !Array.isArray(body) ? Object.keys(body) : undefined };
        console.log(JSON.stringify(summary)); ledger.checks.push(summary);
      } save(); return;
    }
    for (let i=0; i<10; i++) {
      const n = String(i+1).padStart(2,'0'); const [first,last,city,postcode,department,title] = people[i];
      const key = type => `${type}/${n}`;
      const add = (type, url, data, method) => retained(key(type), () => send(url, data, method));
      if (phase === 'crm') {
        const contact = await add('contacts','/api/v1/crm/records',{kind:'contact', name:`${prefix} ${first} ${last}`,status:'active',category:'E2E review'});
        const account = await add('accounts','/api/v1/crm/records',{kind:'account', name:`${prefix} ${city} Professional Network`,status:'active',category:'Professional association'});
        await add('addresses',`/api/v1/crm/records/${account.id}/addresses`,{label:'Registered office',line1:`${10+i} Association Way`,line2:'Review data - fictional address',city,region:'',postalCode:postcode,country:'United Kingdom'});
        await add('communications',`/api/v1/crm/records/${contact.id}/communications`,{kind:'email',value:`journey.${n}@example.test`,consent:'granted',consentEvidence:'Synthetic review persona; consent captured for local audience testing only.'});
        await add('phoneNumbers',`/api/v1/crm/records/${contact.id}/communications`,{kind:'phone',value:`07700900${100+i}`,consent:'unknown',consentEvidence:null});
        await add('notes',`/api/v1/crm/records/${contact.id}/notes`,{body:`${prefix}: ${title} in ${department}. Review profile, membership, event attendance and finance links. Fictional local test record.`});
        await add('tags',`/api/v1/crm/records/${contact.id}/tags`,{name:'Journey 20261001 review'});
        await add('relationships',`/api/v1/crm/records/${contact.id}/relationships`,{targetRecordId:account.id,label:'Professional network affiliation'});
        await add('accountContacts',`/api/v1/crm/accounts/${account.id}/contacts`,{contactId:contact.id,startDate:'2026-10-01',workClassification:'unknown',isMainContact:true,isAuthorisedContact:true,isMainAccount:true,department,jobTitle:title,roles:['membership']});
        const grant = await add('portalAccess','/api/v1/admin/portal-access/local-member',{contactId:contact.id,email:`journey.${n}@example.test`,password});
        await add('organisationGrants',`/api/v1/admin/portal-access/${grant.id}/organisations`,{accountId:account.id,canSubmitApplications:true});
        await add('savedViews','/api/v1/crm/views',{name:`${prefix} ${city} review`,kind:'contact',search:`${first} ${last}`,status:'active',sort:'name',archived:false,columns:'name,status,category'});
        const product = await add('membershipProducts','/api/v1/membership/products',{name:`${prefix} ${topics[i]} Membership`,category:'Professional',termMonths:12,rate:50+i*10,currency:'GBP',isActive:true});
        await add('portalProductSettings',`/api/v1/membership/products/${product.id}/portal`,{version:product.version,enabled:true,organisationEnabled:true,terms:`${prefix}: Annual professional membership. Staff review is required. Quoted fees are review data; no payment is collected online.`},'PUT');
        await add('membershipPolicies',`/api/v1/membership/products/${product.id}/policy`,{version:null,renewalMode:'rolling',yearEndMonth:12,yearEndDay:31,prorate:false,processingFee:0,credit:0,requirePayment:false,requireCompliance:false,requireDeclaration:true},'PUT');
        await add('tasks','/api/v1/work/items',{title:`${prefix} ${n}: Review ${first} ${last} journey`,description:`Check Portal profile and application, ${city} account relationship, event registration, sales invoice and receipt. Keep results for user review.`,status:'open',priority:i<3?'high':'normal',dueAtUtc:'2026-10-15T16:00:00Z',contactId:contact.id});
      }
      if (phase === 'events') {
        const contact=ledger.records[key('contacts')];
        for (const kind of ['event','course','exam']) {
          const offering=await add(`${kind}s`,'/api/v1/events/offerings',{kind,name:`${prefix} ${topics[i]} ${kind}`,venue:`${city} Learning Centre`,startsAtUtc:date(i),endsAtUtc:date(i,16),capacity:20+i,passingScore:60});
          await add(`${kind}Sessions`,`/api/v1/events/offerings/${offering.id}/sessions`,{name:`${topics[i]} practical session`,startsAtUtc:date(i,11),endsAtUtc:date(i,13)});
          let enrollment=await add(`${kind}Enrollments`,`/api/v1/events/offerings/${offering.id}/enrollments`,{contactId:contact.id});
          if(kind==='course') enrollment=await add('courseApprovals',`/api/v1/events/enrollments/${enrollment.id}/approve`,{version:enrollment.version,reason:'Reviewed experience and course prerequisites.'});
          if(kind==='exam') {
            enrollment=await add('examAttendance',`/api/v1/events/enrollments/${enrollment.id}/attend`,{version:enrollment.version,reason:'Local test scenario: attendance recorded to test result entry.'});
            await add('examResults',`/api/v1/events/enrollments/${enrollment.id}/result`,{version:enrollment.version,score:55+i*4});
          }
        }
        const event=ledger.records[key('events')];
        const page=await add('cmsPages','/api/v1/content/pages',{slug:`journey-20261001-event-${n}`,title:event.name,description:`${topics[i]} in ${city}. Local review event; staff-managed registration.`,blocks:[{type:'hero',heading:topics[i],text:`Join our professional learning session in ${city} on ${date(i).slice(0,10)} from 10:00 to 16:00 UTC.`},{type:'text',heading:'Event details',text:`Venue: ${event.venue}. Capacity: ${event.capacity}. Reference: ${event.id}. Fictional local review event. Portal event booking is not implemented; enrollment is currently managed by staff.`},{type:'cta',heading:'Your member profile',text:'Sign in to update your details or apply for membership. Event booking is not available in Portal.',linkLabel:'Open member portal',linkUrl:'/portal'}]});
        // Publishing is exercised in the browser phase, after a saved draft exists.
      }
      if (phase === 'finance') {
        const contact=ledger.records[key('contacts')];
        const product=await add('catalogueProducts','/api/v1/finance/products',{sku:`J261001-${n}`,name:`${prefix} ${topics[i]} workbook`,unitPrice:25+i*5,currency:'GBP',vatBasisPoints:2000});
        const order=await add('orders','/api/v1/finance/orders',{customerId:contact.id,lines:[{productId:product.id,quantity:2}]});
        const invoice=await add('invoices',`/api/v1/finance/orders/${order.id}/post`,{version:order.version});
        await add('credits',`/api/v1/finance/invoices/${invoice.id}/credits`,{amount:5,reference:`J261001-CR-${n}`,reason:'Demonstration goodwill adjustment for review.'});
        const receipt=await add('receipts','/api/v1/finance/receipts',{customerId:contact.id,amount:invoice.total+10,currency:'GBP',reference:`J261001-RC-${n}`});
        await add('allocations','/api/v1/finance/allocations',{receiptId:receipt.id,invoiceId:invoice.id,amount:invoice.total-5});
        await add('refunds',`/api/v1/finance/receipts/${receipt.id}/refunds`,{amount:15,reference:`J261001-RF-${n}`,reason:'Return sample overpayment and credited amount; local ledger only.'});
        const latest=await get(`/api/v1/finance/receipts/${receipt.id}`);
        await add('reconciliations',`/api/v1/finance/receipts/${receipt.id}/reconcile`,{version:latest.receipt.version,statementReference:`J261001-ST-${n}`,amount:receipt.amount,currency:'GBP'});
        const settled=await get(`/api/v1/finance/invoices/${invoice.id}`); const funds=await get(`/api/v1/finance/receipts/${receipt.id}`);
        if(settled.balance!==0||funds.available!==0) throw Error(`Finance imbalance ${n}`);
        check(`Finance ${n}: invoice and receipt reconciled`,{invoiceId:invoice.id,balance:settled.balance,available:funds.available});
      }
      if (phase === 'engagement') {
        const contact=ledger.records[key('contacts')];
        for(const kind of ['group','committee']) {
          const group=await add(`${kind}s`,'/api/v1/engagement/communities',{kind,name:`${prefix} ${topics[i]} ${kind}`,purpose:`Support ${department.toLowerCase()} practice and share professional experience.`});
          await add(`${kind}Members`,`/api/v1/engagement/communities/${group.id}/members`,{contactId:contact.id,role:kind==='committee'?'Secretary':'Member'});
          await add(`${kind}Meetings`,`/api/v1/engagement/communities/${group.id}/meetings`,{subject:`${prefix} ${topics[i]} planning`,agenda:'Review objectives; agree learning priorities; record actions and next meeting.',startsAtUtc:date(i,9)});
        }
        const group=ledger.records[key('groups')];
        const campaign=await add('campaigns','/api/v1/engagement/campaigns',{communityId:group.id,name:`${prefix} ${topics[i]} update`,subject:`Your ${topics[i].toLowerCase()} community update`});
        await add('campaignPreparation',`/api/v1/engagement/campaigns/${campaign.id}/prepare`,{version:campaign.version});
        const fund=await add('funds','/api/v1/engagement/funds',{name:`${prefix} ${topics[i]} bursary`,currency:'GBP',target:5000+i*500});
        for(const kind of ['donation','pledge']) await add(`${kind}s`,`/api/v1/engagement/funds/${fund.id}/contributions`,{contactId:contact.id,kind,amount:kind==='donation'?50+i*5:100+i*10,reference:`J261001-${kind}-${n}`});
        const batch=await add('importBatches','/api/v1/work/imports/preview',{name:`${prefix} ${n} review checklist`,rows:[{title:`${prefix} ${n}: Verify ${topics[i]} evidence`,priority:'normal',dueAtUtc:'2026-10-16T16:00:00Z'}]});
        await add('importCommits',`/api/v1/work/imports/${batch.batch.id}/commit`,{version:batch.batch.version});
      }
      console.log(`${phase}: ${n}/10 saved`);
    }
    check(`${phase} completed`, { records: Object.keys(ledger.records).length });
  } catch(error) { ledger.failures.push({phase,message:error.message,at:new Date().toISOString()}); save(); throw error; }
  finally { await api.dispose(); }
}
if(process.argv[1] && path.resolve(process.argv[1])===import.meta.filename) await main();
