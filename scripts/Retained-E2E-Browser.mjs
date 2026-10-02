import fs from 'node:fs';
import path from 'node:path';
import { chromium, expect as baseExpect, connect, password, prefix, people, topics, output, ledger, retained, check, save } from './Retained-E2E.mjs';
const expect=baseExpect.configure({timeout:45000});
const {api,get,send}=await connect();
const phase=process.argv[2]??'portal';
const browser=await chromium.launch({channel:'msedge',headless:true});
const staff=await browser.newContext({storageState:await api.storageState(),viewport:{width:1440,height:1050}});
const staffPage=await staff.newPage();
staffPage.setDefaultTimeout(45000); staffPage.setDefaultNavigationTimeout(120000);
let activePage=staffPage;
const errors=[];
function observe(page){ page.on('pageerror',error=>errors.push({url:page.url(),message:error.message})); page.on('response',r=>{if(r.status()>=500)errors.push({url:r.url(),status:r.status()});}); }
observe(staffPage);
try {
  if(phase==='cms') {
    await staffPage.goto('http://localhost:3019/website');
    for(let i=0;i<10;i++) {
      const n=String(i+1).padStart(2,'0'); const item=ledger.records[`cmsPages/${n}`];
      await retained(`cmsPublished/${n}`,async()=>{
        await staffPage.getByRole('region',{name:'Website pages',exact:true}).getByRole('button').filter({hasText:item.revision.title}).click();
        await expect(staffPage.getByLabel('Page title',{exact:true})).toHaveValue(item.revision.title);
        await staffPage.getByRole('button',{name:'Preview draft',exact:true}).click();
        await expect(staffPage.getByRole('heading',{name:topics[i],exact:true})).toBeVisible();
        await staffPage.getByRole('button',{name:'Back to editor',exact:true}).click();
        await staffPage.getByRole('button',{name:'Publish saved draft',exact:true}).click();
        await expect(staffPage.getByRole('status').filter({hasText:'Saved version published.'})).toBeVisible();
        const result=await get(`/api/v1/content/pages/${item.page.id}`);
        expect(result.page.publishedId).toBe(result.page.draftId);
        const visitor=await browser.newPage(); observe(visitor);
        await visitor.goto(`http://localhost:3019/site/${result.page.siteId}/${result.page.slug}`);
        await expect(visitor.getByRole('heading',{name:topics[i],exact:true})).toBeVisible();
        await visitor.reload();
        await expect(visitor.getByText('Event details',{exact:true})).toBeVisible();
        await visitor.screenshot({path:path.join(output,`cms-event-${n}.png`),fullPage:true});
        await visitor.close();
        check(`CMS ${n}: preview, publish and anonymous reload`,{pageId:item.page.id});
        return result.page;
      });
    }
  }
  if(phase==='portal') {
    for(let i=0;i<10;i++) {
      const n=String(i+1).padStart(2,'0'); const [first,last,city,postcode,department]=people[i];
      const contact=ledger.records[`contacts/${n}`],account=ledger.records[`accounts/${n}`],product=ledger.records[`membershipProducts/${n}`];
      const member=await browser.newContext({viewport:{width:1280,height:950}}); const page=await member.newPage(); activePage=page; observe(page);
      page.setDefaultTimeout(45000);page.setDefaultNavigationTimeout(120000);
      await page.goto('http://localhost:3018/portal/profile');
      await page.getByLabel('Email',{exact:true}).fill(`journey.${n}@example.test`);
      await page.getByLabel('Password',{exact:true}).fill(password);
      await page.getByRole('button',{name:'Sign in',exact:true}).click();
      await expect(page.getByLabel('Display name',{exact:true})).toHaveValue(contact.name);
      await retained(`portalProfiles/${n}`,async()=>{
        await page.getByLabel('Title',{exact:true}).fill(i===9?'Dr':'');
        await page.getByLabel('First name',{exact:true}).fill(first);
        await page.getByLabel('Last name',{exact:true}).fill(last);
        await page.getByLabel('Address line 1',{exact:true}).fill(`${20+i} Review Crescent`);
        await page.getByLabel('City',{exact:true}).fill(city);
        await page.getByLabel('Postal code',{exact:true}).fill(postcode);
        await page.getByLabel('Country',{exact:true}).fill('United Kingdom');
        await page.getByRole('button',{name:'Save profile',exact:true}).click();
        await expect(page.getByText('Your profile has been saved.',{exact:true})).toBeVisible();
        await page.reload();
        await expect(page.getByLabel('First name',{exact:true})).toHaveValue(first);
        await expect(page.getByLabel('Address line 1',{exact:true})).toHaveValue(`${20+i} Review Crescent`);
        const shared=await get(`/api/v1/crm/records/${contact.id}`);
        expect(shared.contactDetails.firstName).toBe(first);
        expect(shared.addresses.some(a=>a.line1===`${20+i} Review Crescent`)).toBeTruthy();
        await page.screenshot({path:path.join(output,`portal-profile-${n}.png`),fullPage:true});
        check(`Portal ${n}: profile form persisted into CRM`,{contactId:contact.id});
        return {contactId:contact.id,firstName:first};
      });
      for(const scope of ['personal','organisation']) {
        const recordKey=`${scope}Applications/${n}`;
        if(!ledger.records[recordKey]) {
          await page.goto(`http://localhost:3018/portal/${scope==='personal'?'applications':'organisations'}`);
          if(scope==='organisation') await page.getByLabel('Organisation',{exact:true}).selectOption(account.id);
          const region=page.getByRole('region',{name:scope==='personal'?'Membership application':'Organisation membership application',exact:true});
          const existing=await get(`/api/v1/membership/applications?search=${encodeURIComponent(prefix)}&pageSize=100`);
          const prior=existing.items.find(x=>x.productId===product.id && (scope==='personal' ? !x.accountId && x.contactId===contact.id : x.accountId===account.id));
          if(prior) {ledger.records[recordKey]=prior;save();}
          else await retained(recordKey,async()=>{
            await region.getByRole('button',{name:'New application',exact:true}).click();
            await region.getByLabel('Membership product',{exact:true}).selectOption(product.id);
            await region.getByLabel('Application notes',{exact:true}).fill(`${prefix} ${n} ${scope}: ${first} ${last} requests membership for ${department.toLowerCase()} professional development. Please review the saved profile and organisation relationship.`);
            await region.getByRole('button',{name:'Save and review draft',exact:true}).click();
            await expect(region.getByRole('heading',{name:'Review your application',exact:true})).toBeVisible();
            await expect(region.getByRole('button',{name:'Submit application',exact:true})).toBeDisabled();
            await region.getByLabel('I accept these membership terms.',{exact:true}).check();
            await region.getByRole('button',{name:'Submit application',exact:true}).click();
            await expect(region.getByRole('status').filter({hasText:'Application submitted for review.'})).toBeVisible();
            await region.getByRole('link',{name:'View submitted application receipt',exact:true}).click();
            await expect(page.getByRole('heading',{name:'Application receipt',exact:true})).toBeVisible();
            await page.reload();
            await expect(page.getByText('Awaiting review',{exact:true})).toBeVisible();
            const id=new URL(page.url()).pathname.split('/').at(-1);
            const applications=await get('/api/v1/membership/applications?pageSize=100');
            const application=applications.items.find(x=>x.id===id);
            expect(application).toBeTruthy();
            await page.screenshot({path:path.join(output,`portal-${scope}-receipt-${n}.png`),fullPage:true});
            check(`Portal ${n}: ${scope} submitted through form and found in staff records`,{applicationId:id});
            return application;
          });
        }
        const application=ledger.records[recordKey];
        await retained(`${scope}Decisions/${n}`,async()=>{
          const current=(await get('/api/v1/membership/applications?pageSize=100')).items.find(x=>x.id===application.id);
          if(current?.status==='approved') return current;
          expect(current?.status).toBe('submitted');
          return send(`/api/v1/membership/applications/${application.id}/decision`,{version:current.version,decision:'approve',reason:`${prefix} staff review complete. Private audit note; not for the Portal.`});
        });
        await page.goto(`http://localhost:3018/portal/applications/${application.id}${scope==='organisation'?`?organisation=${account.id}`:''}`);
        await expect(page.getByRole('region',{name:'Resulting membership',exact:true})).toContainText('active');
        await expect(page.getByText(`${prefix} staff review complete. Private audit note; not for the Portal.`,{exact:true})).toHaveCount(0);
        check(`Portal ${n}: ${scope} approval visible, private notes hidden`,{applicationId:application.id});
      }
      await page.goto('http://localhost:3018/portal/notifications');
      const inbox=page.getByRole('region',{name:'Notifications',exact:true});
      await expect(inbox).toContainText('approved');
      const unread=inbox.getByRole('button',{name:/^Mark as read/});
      if(await unread.count()) { const count=await unread.count(); await unread.first().click(); await expect(unread).toHaveCount(count-1); await page.reload(); await expect(unread).toHaveCount(count-1); }
      expect((await page.request.get('http://localhost:3018/api/v1/crm/records')).status()).toBe(401);
      const other=ledger.records[`accounts/${String((i+1)%10+1).padStart(2,'0')}`];
      expect((await page.request.get(`http://localhost:3018/api/v1/portal/organisations/${other.id}/profile`)).status()).toBe(404);
      await page.setViewportSize({width:390,height:844});
      expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBeTruthy();
      await page.screenshot({path:path.join(output,`portal-notifications-mobile-${n}.png`),fullPage:true});
      check(`Portal ${n}: notifications, mobile and access isolation`,{});
      await member.close(); activePage=staffPage;
      console.log(`Portal ${n}/10 completed`);
    }
  }
  if(phase==='staff') {
    for(let i=0;i<10;i++) {
      const n=String(i+1).padStart(2,'0'), account=ledger.records[`accounts/${n}`],contact=ledger.records[`contacts/${n}`];
      await retained(`accountProfiles/${n}`,async()=>{
        await staffPage.goto(`http://localhost:3017/relationships/accounts?search=${encodeURIComponent(account.name)}`);
        await staffPage.getByRole('button',{name:`Edit ${account.name}`,exact:true}).click();
        await staffPage.getByLabel('Full organisation name',{exact:true}).fill(`${people[i][2]} Professional Network Limited`);
        await staffPage.getByLabel('Trading as',{exact:true}).fill(`${people[i][2]} Professional Network`);
        await staffPage.getByLabel('Business commencement date',{exact:true}).fill(`${2005+i}-04-01`);
        await staffPage.getByRole('button',{name:'Save organisation details',exact:true}).click();
        await expect.poll(async()=>(await get(`/api/v1/crm/records/${account.id}`)).accountDetails?.tradingAs).toBe(`${people[i][2]} Professional Network`);
        await staffPage.reload();
        await expect(staffPage.getByLabel('Trading as',{exact:true})).toHaveValue(`${people[i][2]} Professional Network`);
        const detail=await get(`/api/v1/crm/records/${account.id}`);
        check(`CRM ${n}: organisation profile form persisted`,{accountId:account.id});
        return detail.accountDetails;
      });
      await staffPage.goto(`http://localhost:3017/relationships/contacts?record=${contact.id}`);
      await expect(staffPage.getByLabel('First name',{exact:true})).toHaveValue(people[i][0]);
      await staffPage.screenshot({path:path.join(output,`crm-connected-contact-${n}.png`),fullPage:true});
    }
  }
  ledger.browserErrors=errors;save();check(`Browser ${phase} complete`,{errors});
} catch(error) {
  await activePage.screenshot({path:path.join(output,`${phase}-failure.png`),fullPage:true}).catch(()=>{});
  fs.writeFileSync(path.join(output,`${phase}-failure.txt`),(await activePage.locator('body').innerText().catch(()=>''))+'\n'+error.stack);
  ledger.failures.push({phase:`browser-${phase}`,message:error.message,errors,at:new Date().toISOString()});save();throw error;
} finally {await browser.close();await api.dispose();}
