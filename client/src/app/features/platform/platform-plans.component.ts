import { LocalDatePipe } from '../../core/i18n/format.pipe';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { FEATURE_LABELS } from './platform-public.service';
import { ApiService } from '../../core/services/api.service';

@Component({
  standalone: true, imports: [LocalDatePipe,TranslatePipe,CommonModule, FormsModule, RouterLink],
  template: `
  <h1>{{ 'Merchant plans and subscriptions' | t }}</h1>
  <a routerLink="/platform">{{ 'Back to platform' | t }}</a>
  <p>{{ 'Activation and renewal are manual. No automatic payments are collected.' | t }}</p>
  <p role="alert" *ngIf="error">{{ (error) | t }}</p><p role="status" *ngIf="message">{{ (message) | t }}</p>
  <section class="card">
    <h2>{{ 'Plans' | t }}</h2>
    <p>{{ 'Draft plans are not available for activation. Set the price and limits before enabling a plan.' | t }}</p>
    <table><thead><tr><th>{{ 'Name' | t }}</th><th>{{ 'Price / period' | t }}</th><th>{{ 'Product limit' | t }}</th><th>{{ 'Trial' | t }}</th><th>{{ 'Available' | t }}</th><th></th></tr></thead>
    <tbody><tr *ngFor="let p of plans"><td>{{ (p.name) | t }}</td><td>{{ (p.priceMinor / 100 | currency:p.currency) | t }} / {{ (p.periodMonths) | t }} {{ 'month(s)' | t }}</td><td>{{ (p.maxProducts ?? 'Unlimited') | t }}</td><td>{{ (p.trialDays) | t }} {{ 'days' | t }}</td><td>{{ (p.isActive ? 'Active' : 'Draft / disabled') | t }}</td><td><button (click)="edit(p)">{{ 'Edit' | t }}</button></td></tr></tbody></table>
    <form class="package-editor" (ngSubmit)="save()">
      <h3>{{ (draft.id ? 'Edit plan' : 'Add plan') | t }}</h3>
      <div class="package-grid">
      <label>{{ 'Name' | t }} <input name="name" [(ngModel)]="draft.name" required maxlength="100"></label>
      <label>{{ 'Arabic plan name (optional)'|t }} <input name="nameAr" [(ngModel)]="draft.nameAr" maxlength="100" dir="rtl"></label>
      <label>{{ 'Description'|t }} <textarea name="description" [(ngModel)]="draft.description" maxlength="500" rows="3"></textarea></label>
      <label>{{ 'Arabic plan description (optional)'|t }} <textarea name="descriptionAr" [(ngModel)]="draft.descriptionAr" maxlength="500" rows="3" dir="rtl"></textarea></label>
      <label>{{ 'Price' | t }} <input name="price" type="number" [(ngModel)]="price" min="0" max="1000000000" step="0.01" required></label>
      <label>{{ 'Currency' | t }} <select name="currency" [(ngModel)]="draft.currency"><option *ngFor="let code of currencies">{{ code }}</option></select></label>
      <label>{{ 'Billing period (months)' | t }} <input name="period" type="number" [(ngModel)]="draft.periodMonths" min="1" max="36" required></label>
      <label>{{ 'Trial (days)' | t }} <input name="trial" type="number" [(ngModel)]="draft.trialDays" min="0" max="90" required></label>
      <label>{{ 'Maximum products (blank = unlimited)' | t }} <input name="limit" type="number" [(ngModel)]="draft.maxProducts" min="1" max="10000000"></label>
      </div>
      <label><input name="active" type="checkbox" [(ngModel)]="draft.isActive"> {{ 'Available for new activations and renewals' | t }}</label>
      <label><input name="publicTrial" type="checkbox" [(ngModel)]="draft.allowMerchantApplications"> {{ 'Publish this plan on the website and registration' | t }}</label>
      <p>{{ 'Published active plans appear on the website. Paid plans require manual activation until online payments are connected.' | t }}</p>
      <fieldset><legend>{{ 'Included features'|t }}</legend><p>{{ 'Products, inventory and order management are included in every plan.'|t }}</p><label *ngFor="let f of featureKeys"><input type="checkbox" [name]="'feature-'+f" [(ngModel)]="enabled[f]"> {{ featureLabels[f]|t }}</label></fieldset>
      <p>{{ 'Saving changes does not alter existing subscriptions. New terms apply on the next explicit activation or renewal.' | t }}</p>
      <button class="btn btn-primary" [disabled]="busy">{{ 'Save plan' | t }}</button> <button type="button" (click)="reset()">{{ 'New plan' | t }}</button>
    </form>
  </section>
  <section class="card">
    <h2>{{ 'Store subscriptions' | t }}</h2>
    <table><thead><tr><th>{{ 'Store' | t }}</th><th>{{ 'Status' | t }}</th><th>{{ 'Plan' | t }}</th><th>{{ 'Valid until' | t }}</th><th></th></tr></thead>
    <tbody><tr *ngFor="let s of stores"><td>{{ (s.name) | t }}</td><td>{{ (s.status) | t }}</td><td>{{ (s.subscription?.planName || 'Unassigned') | t }}</td><td>{{ (s.subscription?.endsAtUtc |localDate:'medium') | t }}</td><td><button (click)="selectStore(s)">{{ 'Manage' | t }}</button></td></tr></tbody></table>
    <form *ngIf="selected" (ngSubmit)="change()">
      <h3>{{ (selected.name) | t }}</h3>
      <p>{{ 'Assign replaces the current terms immediately. Renewal adds a period to the current paid expiry. Cancellation stops selling immediately without deleting data.' | t }}</p>
      <label>{{ 'Action' | t }} <select name="action" [(ngModel)]="action"><option value="Assign">{{ 'Activate / change plan manually' | t }}</option><option value="Trial">{{ 'Start one-time trial' | t }}</option><option value="Renew">{{ 'Renew existing plan manually' | t }}</option><option value="Cancel">{{ 'Cancel immediately' | t }}</option></select></label>
      <label *ngIf="action !== 'Cancel'">{{ 'Plan' | t }} <select name="plan" [(ngModel)]="planId" required><option *ngFor="let p of activePlans" [ngValue]="p.id">{{ (p.name) | t }} — {{ (p.priceMinor / 100 | currency:p.currency) | t }} / {{ (p.periodMonths) | t }} {{ 'month(s)' | t }}</option></select></label>
      <label>{{ 'Administrative reference / note' | t }} <input name="reference" [(ngModel)]="reference" maxlength="200"></label>
      <button class="btn btn-primary" [disabled]="busy || (action !== 'Cancel' && !planId)">{{ 'Apply subscription action' | t }}</button>
      <h3>{{ 'Subscription history' | t }}</h3>
      <ul><li *ngFor="let e of history">{{ (e.createdAtUtc |localDate:'medium') | t }} · {{ (e.action) | t }} · {{ (e.planName) | t }} {{ '· until' | t }} {{ (e.endsAtUtc |localDate:'medium') | t }} <span *ngIf="e.reference">· {{ (e.reference) | t }}</span></li></ul>
    </form>
  </section>`,
  styles: [`:host{display:block;max-width:1100px;margin:1rem auto}.card{padding:1.5rem;margin:1.5rem 0}table{width:100%;text-align:start}th,td{padding:.65rem;border-bottom:1px solid #ddd}label{display:block;margin:.8rem 0}input:not([type=checkbox]),select{padding:.6rem;display:block;max-width:480px;width:100%}[role=alert]{color:#a22}form{margin-top:1rem}section{overflow:auto}`]
})
export class PlatformPlansComponent {
  featureLabels=FEATURE_LABELS;featureKeys=Object.keys(FEATURE_LABELS);enabled:Record<string,boolean>={};
  plans: any[] = []; stores: any[] = []; history: any[] = [];
  currencies = ['EGP','USD','EUR','GBP','SAR','AED'];
  draft: any = {}; price = 0; busy = false; error = ''; message = '';
  selected: any = null; action = 'Assign'; planId: number | null = null; reference = '';
  get activePlans(): any[] { return this.plans.filter(p => p.isActive); }
  constructor(private api: ApiService) { this.reset(); this.load(); }
  reset(): void { this.enabled=Object.fromEntries(this.featureKeys.map(f=>[f,true])); this.draft = {name:'',currency:'EGP',periodMonths:1,trialDays:0,maxProducts:null,isActive:false,allowMerchantApplications:false,version:0}; this.price=0; }
  edit(plan: any): void { let chosen:string[]=[];try{chosen=JSON.parse(plan.featuresJson);}catch{}this.enabled=Object.fromEntries(this.featureKeys.map(f=>[f,chosen.includes(f)]));this.draft={...plan}; this.price=plan.priceMinor / 100; this.message=''; }
  fail(err: any): void { this.busy=false; this.error=err.error?.message || 'Unable to complete the action. Sign in to the platform or refresh and try again.'; }
  load(): void {
    this.api.get<any[]>('platform/plans').subscribe({next: plans => this.plans=plans,error: err => this.fail(err)});
    this.api.get<any[]>('platform/subscriptions').subscribe({next: stores => { this.stores=stores; if(this.selected) this.selected=stores.find(s => s.id===this.selected.id); },error: err => this.fail(err)});
  }
  save(): void {
    if(!Number.isFinite(this.price) || this.price < 0) { this.error='Enter a valid price.'; return; }
    this.busy=true; this.error='';
    const body={...this.draft,features:this.featureKeys.filter(f=>this.enabled[f]),priceMinor:Math.round(this.price*100)};
    const request=this.draft.id ? this.api.put('platform/plans/'+this.draft.id,body) : this.api.post('platform/plans',body);
    request.subscribe({next: () => { this.busy=false; this.message='Plan saved. Existing subscriptions retain their agreed terms.'; this.reset(); this.load(); },error: err => this.fail(err)});
  }
  selectStore(store: any): void { this.selected=store; this.planId=store.subscription?.planId || null; this.action='Assign'; this.reference=''; this.loadHistory(); }
  loadHistory(): void { this.api.get<any[]>('platform/stores/'+this.selected.id+'/subscription/history').subscribe({next: rows => this.history=rows,error: err => this.fail(err)}); }
  change(): void {
    this.busy=true; this.error=''; this.message='';
    this.api.post('platform/stores/'+this.selected.id+'/subscription',{action:this.action,planId:this.planId || 0,version:this.selected.subscription?.version || 0,reference:this.reference}).subscribe({
      next: () => {this.busy=false;this.message='Subscription updated. No automatic payment was collected.';this.load();this.loadHistory();},error: err => {this.fail(err);this.load();}
    });
  }
}
