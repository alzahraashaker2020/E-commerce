import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PlatformPublicService } from '../platform/platform-public.service';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { RuntimeService } from '../../core/services/runtime.service';
import { ApiService } from '../../core/services/api.service';
interface TrialPlan {nameAr:string;descriptionAr:string;priceMinor:number;currency:string;periodMonths:number;description:string;featuresJson:string;version:number;id:number;name:string;trialDays:number;maxProducts:number|null;}
interface ApplicationStatus {id:string;state:string;storeName:string;username:string;reviewNote:string;host:string|null;}
@Component({standalone:true,imports:[CommonModule,FormsModule,RouterLink,TranslatePipe],
 template:`<main class="onboarding">
 <h1>{{ 'Merchant registration' | t }}</h1>
 <fieldset class="account-type"><legend>{{ 'Account type' | t }}</legend>
 <label><input type="radio" name="accountType" value="customer" (change)="customer.click()">{{ 'Customer account' | t }}</label>
 <label><input type="radio" name="accountType" value="merchant" checked>{{ 'Merchant account' | t }}</label>
 <a #customer [routerLink]="runtime.isPlatformHost?'/platform/shop':'/register/customer'" class="customer-link">{{ (runtime.isPlatformHost?'Shop as a customer':'Create customer account') | t }}</a></fieldset>
 <ng-container *ngIf="runtime.saasEnabled; else standalone">
 <p>{{ 'Apply for your own store. Store management is available after platform approval.' | t }}</p>
 <p class="alert alert-error" role="alert" *ngIf="error">{{ error | t }}</p>
 <p role="status" *ngIf="sent">{{ 'Your application was saved. Check its status below. Your subscription starts after approval.' | t }}</p>
 <form #application="ngForm" (ngSubmit)="submit()" *ngIf="!sent">
 <h2>{{ 'Your details' | t }}</h2>
 <label>{{ 'Full name' | t }}<input name="fullName" [(ngModel)]="draft.fullName" required maxlength="150" autocomplete="name"></label>
 <label>{{ 'Email' | t }}<input name="email" [(ngModel)]="draft.email" type="email" required email maxlength="200" autocomplete="email"></label>
 <label>{{ 'Phone' | t }}<input name="phone" [(ngModel)]="draft.phone" required maxlength="40" autocomplete="tel"></label>
 <label>{{ 'Merchant username' | t }}<input name="username" [(ngModel)]="draft.username" required pattern="[A-Za-z0-9_-]{3,60}" autocomplete="username"></label>
 <p>{{ 'Use 3–60 English letters, numbers, underscores or hyphens for your username.' | t }}</p>
 <label>{{ 'Password' | t }}<input name="password" [(ngModel)]="draft.password" type="password" required minlength="12" maxlength="200" autocomplete="new-password"></label>
 <label>{{ 'Confirm password' | t }}<input name="confirm" [(ngModel)]="confirm" type="password" required autocomplete="new-password"></label>
 <p>{{ 'Use at least 12 characters for your merchant password.' | t }}</p>
 <h2>{{ 'Your store' | t }}</h2>
 <label>{{ 'Store name' | t }}<input name="storeName" [(ngModel)]="draft.storeName" required maxlength="150"></label>
 <label>{{ 'What will you sell?' | t }}<input name="businessCategory" [(ngModel)]="draft.businessCategory" required maxlength="100" [placeholder]="'For example: kids clothing, homewear, furniture' | t"></label>
 <label>{{ 'Tell us about your business' | t }}<textarea name="businessDescription" [(ngModel)]="draft.businessDescription" required maxlength="1000" rows="3"></textarea></label>
 <label>{{ 'Selling mode' | t }}<select name="businessMode" [(ngModel)]="draft.businessMode"><option value="Commerce">{{ 'Online store with orders' | t }}</option><option value="Catalog">{{ 'Display catalog only' | t }}</option></select></label>
 <label>{{ 'Subscription plan' | t }}<select name="planId" [(ngModel)]="draft.planId" required><option [ngValue]="0" disabled>{{ 'Select a plan' | t }}</option><option *ngFor="let p of plans" [ngValue]="p.id">{{ (publicSite.language.arabic?(p.nameAr||p.name):p.name) | t }} · {{ planSummary(p) }}</option></select></label>
 <article *ngIf="selectedPlan as p"><p>{{ publicSite.language.arabic?(p.descriptionAr||p.description):p.description }}</p><p *ngIf="p.trialDays">{{ p.trialDays }} {{ 'trial days after approval'|t }}</p><p>{{ p.maxProducts ?? ('Unlimited'|t) }} {{ 'Products'|t }}</p><ul><li *ngFor="let f of publicSite.features(p.featuresJson)">{{ publicSite.featureLabel(f)|t }}</li></ul></article>
 <p>{{ 'No automatic payment is collected. Paid activations require manual payment verification until online payments are connected.'|t }}</p>
 <p *ngIf="!loading && !plans.length">{{ 'No published plans are currently available. Please contact the platform owner.' | t }}</p>
 <label class="inline"><input type="checkbox" name="acceptTerms" [(ngModel)]="draft.acceptTerms" required>{{ 'I accept the selected plan terms and understand that activation requires platform approval. No automatic payment is collected now.' | t }}</label>
 <button class="btn btn-primary" [disabled]="busy || loading || !application.valid || draft.password!==confirm || !draft.planId">{{ (busy ? 'Saving…' : 'Submit merchant application') | t }}</button>
 </form>
 <section class="status"><h2>{{ 'Check application status' | t }}</h2>
 <form #lookup="ngForm" (ngSubmit)="checkStatus()">
 <label>{{ 'Email' | t }}<input name="statusEmail" [(ngModel)]="statusEmail" type="email" required email autocomplete="username"></label>
 <label>{{ 'Application password' | t }}<input name="statusPassword" [(ngModel)]="statusPassword" type="password" required autocomplete="current-password"></label>
 <button class="btn btn-outline" [disabled]="busy || !lookup.valid">{{ 'Check status' | t }}</button>
 </form>
 <article *ngIf="status" aria-live="polite"><strong>{{ status.state | t }}</strong><p>{{ status.storeName }}</p><p>{{ status.reviewNote | t }}</p>
 <p *ngIf="status.state==='Pending'">{{ 'Your application is awaiting platform review. You cannot sign in as a merchant yet.' | t }}</p>
 <ng-container *ngIf="status.host"><p>{{ 'Your store address' | t }}: {{ status.host }}</p><p>{{ 'Merchant username' | t }}: {{ status.username }}</p><a class="btn btn-primary" [href]="storeLogin()">{{ 'Open your merchant portal' | t }}</a></ng-container></article>
 </section></ng-container>
 <ng-template #standalone><p>{{ 'The store owner account is provided when this standalone store is installed. Contact your system administrator for access.' | t }}</p></ng-template>
 <p *ngIf="!runtime.isPlatformHost"><a routerLink="/merchant/login">{{ 'Merchant sign in' | t }}</a></p>
 </main>`,
 styles:[`.onboarding{max-width:680px;margin:2rem auto;padding:1rem}form,.status{margin:1.5rem 0;padding:1.25rem;border:1px solid #ddd;border-radius:14px;background:white}label{display:block;margin:1rem 0}input:not([type=checkbox]):not([type=radio]),textarea,select{width:100%;padding:.7rem;box-sizing:border-box}.inline,.account-type label{display:flex;align-items:center;gap:.6rem}.inline input{flex-shrink:0}.account-type{display:flex;gap:1rem;flex-wrap:wrap;border:1px solid #ddd;border-radius:10px}.account-type input{width:auto}.customer-link{align-self:center;font-size:.85rem}h1{font-size:1.7rem}h2{font-size:1.2rem}article{padding:1rem;background:#f3f7fc;overflow-wrap:anywhere}`]})
export class MerchantRegistrationComponent {
 readonly runtime=inject(RuntimeService);readonly publicSite=inject(PlatformPublicService);private route=inject(ActivatedRoute);private api=inject(ApiService);
 plans:TrialPlan[]=[];loading=true;busy=false;error='';sent=false;confirm='';statusEmail='';statusPassword='';status:ApplicationStatus|null=null;
 draft={fullName:'',email:'',phone:'',username:'',password:'',storeName:'',businessCategory:'',businessDescription:'',businessMode:'Commerce',planId:0,acceptTerms:false};
 constructor(){if(this.runtime.saasEnabled)this.api.get<TrialPlan[]>('platform/onboarding/plans').subscribe({next:p=>{this.plans=p;const selected=Number(this.route.snapshot.queryParamMap.get('planId'));if(p.some(plan=>plan.id===selected))this.draft.planId=selected;this.loading=false;},error:()=>{this.loading=false;this.error='Unable to load available trial plans.';}});}
 planSummary(p:TrialPlan):string {if(p.priceMinor===0&&p.trialDays>0)return p.trialDays+' '+this.publicSite.language.text('days');return new Intl.NumberFormat(this.publicSite.language.locale,{style:'currency',currency:p.currency}).format(p.priceMinor/100)+' / '+p.periodMonths+' '+this.publicSite.language.text('month(s)');}
 get selectedPlan():TrialPlan|undefined{return this.plans.find(p=>p.id===this.draft.planId);}
 submit():void{if(this.busy||!this.draft.acceptTerms||!this.draft.planId||this.draft.password!==this.confirm)return;
 this.busy=true;this.error='';this.api.post('platform/onboarding/applications',{...this.draft,planVersion:this.plans.find(p=>p.id===this.draft.planId)?.version}).subscribe({next:()=>{this.busy=false;this.sent=true;this.statusEmail=this.draft.email;this.draft.password='';this.confirm='';},error:e=>{this.busy=false;this.error=e.error?.message??'Unable to submit application.';}});}
 checkStatus():void{if(this.busy)return;this.busy=true;this.error='';this.status=null;
 this.api.post<ApplicationStatus>('platform/onboarding/status',{email:this.statusEmail,password:this.statusPassword}).subscribe({next:s=>{this.busy=false;this.status=s;this.statusPassword='';},error:e=>{this.busy=false;this.error=e.error?.message??'Unable to check application status.';}});}
 storeLogin():string{return this.status?.host ? location.protocol+'//'+this.status.host+(location.port?':'+location.port:'')+'/merchant/login' : '/merchant/login';}
}
