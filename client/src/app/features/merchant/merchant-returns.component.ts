import { LocalDatePipe } from '../../core/i18n/format.pipe';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

@Component({standalone:true,imports: [LocalDatePipe,TranslatePipe,CommonModule,FormsModule],template:`
<h1>{{ 'Returns and refunds' | t }}</h1><p *ngIf="error" role="alert">{{ (error) | t }}</p><button class="btn" (click)="load()">{{ 'Refresh' | t }}</button>
<p>{{ 'Inspect received products before restoring stock. Refund recording does not transfer money; make the payment separately and record its reference here.' | t }}</p>
<article *ngFor="let r of rows" class="card" style="padding:1.2rem;margin:1rem 0">
<h2>{{ r.orderNumber }} — {{ (r.state) | t }}</h2><p>{{ (r.productName) | t }} ×{{ (r.quantity) | t }} · {{ (r.reason) | t }}</p>
<p>{{ 'Maximum product refund (shipping excluded):' | t }} {{ (r.maxRefund) | t }} {{ r.currencyCode }}</p><p>{{ 'Restocked:' | t }} {{ (r.restocked?'Yes':'No') | t }} {{ '· Recorded refund:' | t }} {{ (r.refundAmount) | t }} {{ r.currencyCode }}</p><p *ngIf="r.notes">{{ 'Last note:' | t }} {{ (r.notes) | t }}</p>
<ng-container *ngIf="r.state!=='Completed' && r.state!=='Rejected'">
<label>{{ 'Decision / inspection notes' | t }}<textarea [(ngModel)]="r.editNotes" maxlength="1000"></textarea></label>
<ng-container *ngIf="r.state==='Requested'"><button [disabled]="busy" (click)="act(r,'Approve')">{{ 'Approve' | t }}</button> </ng-container>
<button *ngIf="r.state==='Requested' || r.state==='Approved'" [disabled]="busy" (click)="act(r,'Reject')">{{ 'Reject' | t }}</button>
<div *ngIf="r.state==='Approved'"><label><input type="checkbox" [(ngModel)]="r.restore"> {{ 'Inspected and suitable for resale: restore stock' | t }}</label><button [disabled]="busy" (click)="act(r,'Receive')">{{ 'Record receipt and inspection' | t }}</button></div>
<div *ngIf="r.state==='Received'"><label>{{ 'Amount paid back' | t }} <input type="number" [(ngModel)]="r.amount" min="0" [max]="r.maxRefund" step="0.01"></label><label>{{ 'External payment reference' | t }}<input [(ngModel)]="r.reference" maxlength="200"></label><button [disabled]="busy" (click)="act(r,'RecordRefund')">{{ 'Record manual refund and complete' | t }}</button></div>
</ng-container><button (click)="history(r)">{{ 'View history' | t }}</button><ul><li *ngFor="let e of r.history">{{ (e.createdAtUtc |localDate:'short') | t }} — {{ (e.action) | t }} — {{ e.actor }} — {{ (e.notes) | t }}</li></ul>
</article><p *ngIf="!rows.length">{{ 'No return requests.' | t }}</p>`,styles:[`label{display:block;margin:.8rem 0}textarea{display:block;width:100%;min-height:65px}button{margin:.3rem}input{padding:.4rem}`]})
export class MerchantReturnsComponent{
  rows:any[]=[];error='';busy=false;
  constructor(private api:ApiService){this.load();}
  load():void{this.api.get<any[]>('merchant/returns').subscribe({next:r=>this.rows=r,error:()=>this.error='Unable to load returns.'});}
  act(r:any,action:string):void{if(this.busy)return;this.error='';this.busy=true;this.api.post('merchant/returns/'+r.id+'/actions',{version:r.version,action,notes:r.editNotes||'',restock:action==='Receive'&&!!r.restore,refundAmount:action==='RecordRefund'?Number(r.amount||0):0,refundReference:r.reference||''}).subscribe({next:()=>{this.busy=false;this.load();},error:e=>{this.busy=false;this.error=e.error?.message||'Update failed. Refresh before trying again.';}});}
  history(r:any):void{this.api.get<any[]>('merchant/returns/'+r.id+'/history').subscribe({next:h=>r.history=h,error:()=>this.error='Unable to load return history.'});}
}
