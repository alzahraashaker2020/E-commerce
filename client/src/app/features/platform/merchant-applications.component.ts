import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { ApiService } from '../../core/services/api.service';
interface Application {id:string;fullName:string;email:string;phone:string;username:string;storeName:string;businessCategory:string;businessDescription:string;businessMode:string;planId:number;planName:string;trialDays:number;maxProducts:number|null;priceMinor:number;currency:string;periodMonths:number;state:string;reviewNote:string;version:number;storeId:string|null;}
@Component({standalone:true,imports:[CommonModule,FormsModule,RouterLink,TranslatePipe],
 template:`<main class="applications"><a routerLink="/platform">{{ 'Platform administration' | t }}</a><h1>{{ 'Merchant applications' | t }}</h1>
 <p>{{ 'Approve only after checking the applicant and preparing a separate database reference. Trials start on approval.' | t }}</p>
 <p><a routerLink="/platform/plans">{{ 'Manage plans and subscriptions' | t }}</a></p>
 <p role="alert" *ngIf="error">{{ error | t }}</p><p role="status" *ngIf="message">{{ message | t }}</p>
 <button class="btn btn-outline" (click)="load()" [disabled]="busy">{{ 'Refresh' | t }}</button>
 <p *ngIf="!rows.length">{{ 'No merchant applications yet.' | t }}</p>
 <article class="card" *ngFor="let a of rows"><h2>{{ a.storeName }} · {{ a.state | t }}</h2>
 <p>{{ a.fullName }} · {{ a.email }} · {{ a.phone }}</p><p>{{ 'Merchant username' | t }}: {{ a.username }}</p>
 <p>{{ 'Business category' | t }}: {{ a.businessCategory }}</p><p>{{ a.businessDescription }}</p><p>{{ a.businessMode | t }} · {{ a.planName | t }} · {{ a.trialDays }} {{ 'days' | t }} · {{ a.maxProducts ?? ('Unlimited' | t) }} {{ 'Products' | t }}</p>
 <p>{{ a.priceMinor/100|currency:a.currency }} / {{ a.periodMonths }} {{ 'month(s)'|t }}</p>
 <p *ngIf="a.reviewNote">{{ a.reviewNote | t }}</p>
 <form *ngIf="a.state==='Pending'" #review="ngForm" (ngSubmit)="approve(a)">
 <label>{{ 'Store hostname' | t }}<input name="host" [(ngModel)]="drafts[a.id].host" required maxlength="253"></label>
 <label>{{ 'Database reference' | t }}<input name="connectionKey" [(ngModel)]="drafts[a.id].connectionKey" required maxlength="80"></label>
 <p>{{ 'The database reference must be configured on the server. Connection passwords are never entered here.' | t }}</p>
 <ng-container *ngIf="a.priceMinor>0 && a.trialDays===0"><label><input type="checkbox" name="paymentVerified" [(ngModel)]="drafts[a.id].paymentVerified">{{ 'Payment verified manually'|t }}</label><label>{{ 'Payment reference'|t }}<input name="paymentReference" [(ngModel)]="drafts[a.id].paymentReference" required maxlength="200"></label></ng-container>
 <button class="btn btn-primary" [disabled]="busy || !review.valid || (a.priceMinor>0 && a.trialDays===0 && !drafts[a.id].paymentVerified)">{{ 'Approve and create store' | t }}</button>
 <label>{{ 'Review note' | t }}<textarea name="note" [(ngModel)]="drafts[a.id].note" maxlength="500"></textarea></label>
 <button type="button" class="btn btn-outline" (click)="reject(a)" [disabled]="busy || !drafts[a.id].note.trim()">{{ 'Reject application' | t }}</button>
 </form>
 <p *ngIf="a.state==='Failed' || a.state==='Provisioning'">{{ 'Store setup requires operator attention. Do not approve a second store for this application.' | t }}</p>
 </article></main>`,
 styles:[`.applications{max-width:950px;margin:2rem auto;padding:1rem}article{padding:1.25rem;margin:1rem 0;overflow-wrap:anywhere}label{display:block;margin:1rem 0}input,textarea{display:block;width:100%;max-width:600px;padding:.7rem;box-sizing:border-box}[role=alert]{color:#a22}h1{font-size:1.7rem}h2{font-size:1.25rem}`]})
export class MerchantApplicationsComponent{
 private api=inject(ApiService);rows:Application[]=[];busy=false;error='';message='';drafts:Record<string,{host:string;connectionKey:string;note:string;paymentVerified:boolean;paymentReference:string}>={};
 constructor(){this.load();}
 load():void{this.busy=true;this.error='';this.api.get<Application[]>('platform/onboarding/applications').subscribe({next:r=>{this.rows=r;for(const a of r)this.drafts[a.id]??={host:'',connectionKey:'',note:'',paymentVerified:false,paymentReference:''};this.busy=false;},error:e=>{this.busy=false;this.error=e.error?.message??'Unable to load merchant applications.';}});}
 approve(a:Application):void{if(this.busy)return;this.busy=true;this.error='';this.message='';this.api.post('platform/onboarding/applications/'+a.id+'/approve',{version:a.version,host:this.drafts[a.id].host,connectionKey:this.drafts[a.id].connectionKey,paymentVerified:this.drafts[a.id].paymentVerified,paymentReference:this.drafts[a.id].paymentReference}).subscribe({next:()=>{this.message='Merchant approved. The store and subscription are ready.';this.load();},error:e=>{this.error=e.error?.message??'Unable to approve application.';this.busy=false;}});}
 reject(a:Application):void{if(this.busy||!this.drafts[a.id].note.trim())return;this.busy=true;this.error='';this.message='';this.api.post('platform/onboarding/applications/'+a.id+'/reject',{version:a.version,note:this.drafts[a.id].note}).subscribe({next:()=>{this.message='Application rejected.';this.load();},error:e=>{this.error=e.error?.message??'Unable to reject application.';this.busy=false;}});}
}
