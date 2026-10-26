import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Component, Input, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({selector:'app-order-returns',standalone:true,imports: [TranslatePipe,CommonModule,FormsModule],template:`
<section style="margin-top:1.5rem;border-top:1px solid #ddd;padding-top:1rem">
<h3>{{ 'Returns' | t }}</h3><p *ngIf="error" role="alert">{{ (error) | t }}</p>
<ng-container *ngIf="data"><p *ngIf="!data.eligible">{{ 'Returns are available after delivery.' | t }}</p>
<form *ngIf="data.eligible" (ngSubmit)="submit()">
<fieldset [disabled]="busy || !!pending"><legend>{{ 'Request a return' | t }}</legend>
<label>{{ 'Item' | t }} <select name="item" [(ngModel)]="itemId" required><option [ngValue]="0">{{ 'Select an item' | t }}</option><option *ngFor="let item of data.items" [ngValue]="item.id">{{ (item.productNameSnapshot) | t }} {{ (item.sizeSnapshot) | t }} {{ (item.colorSnapshot) | t }} {{ '(purchased:' | t }} {{ (item.quantity) | t }})</option></select></label>
<label>{{ 'Quantity' | t }} <input type="number" name="quantity" [(ngModel)]="quantity" min="1" [max]="selectedQuantity()" required></label>
<label>{{ 'Reason' | t }} <textarea name="reason" [(ngModel)]="reason" minlength="3" maxlength="1000" required></textarea></label>
</fieldset><button class="btn" [disabled]="busy || (!pending && (!itemId || reason.trim().length<3 || quantity<1))">{{ (pending?'Retry pending request':'Submit return request') | t }}</button>
<button type="button" *ngIf="pending && !busy" (click)="edit()">{{ 'Review form' | t }}</button>
<p>{{ 'Approval and refund depend on merchant inspection. Shipping is excluded from the suggested product refund. A recorded refund means the merchant has recorded a separate payment.' | t }}</p>
</form>
<article *ngFor="let r of data.requests" style="padding:.7rem 0"><strong>{{ (r.productName) | t }} ×{{ (r.quantity) | t }} — {{ (r.state) | t }}</strong><p>{{ (r.reason) | t }}</p><p *ngIf="r.notes">{{ 'Merchant:' | t }} {{ (r.notes) | t }}</p><p *ngIf="r.state==='Received' || r.state==='Completed'">{{ 'Stock restored:' | t }} {{ (r.restocked?'Yes':'No') | t }} {{ '· Recorded refund:' | t }} {{ (r.refundAmount) | t }} {{ r.currencyCode }}</p></article>
<p *ngIf="!data.requests.length">{{ 'No return requests.' | t }}</p></ng-container>
<button type="button" class="btn" (click)="load()">{{ 'Refresh returns' | t }}</button></section>
`,styles:[`label{display:block;margin:.6rem 0}textarea,select,input{display:block;max-width:100%;padding:.5rem}textarea{width:100%}fieldset{border:0;padding:0}`]})
export class OrderReturnsComponent implements OnChanges{
  @Input() orderNumber='';@Input() trackingCode='';
  data:any;error='';busy=false;itemId=0;quantity=1;reason='';pending:any=null;
  constructor(private http:HttpClient){}
  private key():string{return 'shop_return_pending:'+location.hostname+':'+this.orderNumber;}
  private url():string{return environment.apiBaseUrl+'/returns/'+encodeURIComponent(this.orderNumber);}
  private headers():HttpHeaders{return this.trackingCode?new HttpHeaders({'X-Order-Tracking-Token':this.trackingCode}):new HttpHeaders();}
  ngOnChanges():void{this.data=null;try{this.pending=JSON.parse(sessionStorage.getItem(this.key())||'null');}catch{this.pending=null;}this.load();}
  selectedQuantity():number{return this.data?.items.find((i:any)=>i.id===this.itemId)?.quantity||1;}
  load():void{if(!this.orderNumber)return;this.http.get<any>(this.url(),{headers:this.headers()}).subscribe({next:r=>this.data=r.data,error:()=>this.error='Unable to load returns for this order.'});}
  edit():void{this.pending=null;sessionStorage.removeItem(this.key());this.error='Review existing requests before submitting another quantity.';this.load();}
  submit():void{
    if(this.busy)return;
    if(!this.pending){this.pending={requestKey:crypto.randomUUID(),orderItemId:this.itemId,quantity:this.quantity,reason:this.reason.trim()};sessionStorage.setItem(this.key(),JSON.stringify(this.pending));}
    this.busy=true;this.error='';this.http.post<any>(this.url(),this.pending,{headers:this.headers()}).subscribe({next:()=>{this.busy=false;this.pending=null;sessionStorage.removeItem(this.key());this.reason='';this.load();},error:e=>{this.busy=false;this.error=e.error?.message||'Could not confirm the result. Retry the same request.';}});
  }
}
